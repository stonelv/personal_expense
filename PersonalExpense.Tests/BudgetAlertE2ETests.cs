using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Tests;

public class BudgetAlertE2ETests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ApplicationDbContext _context;
    private readonly BudgetService _budgetService;
    private readonly TransactionService _transactionService;

    public BudgetAlertE2ETests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _budgetService = new BudgetService(_context);
        _transactionService = new TransactionService(_context, _budgetService);
    }

    private async Task<Account> CreateTestAccountAsync(string name, decimal initialBalance)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = AccountType.Cash,
            Balance = initialBalance,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserId = _userId
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    private async Task<Category> CreateTestCategoryAsync(string name, CategoryType type)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserId = _userId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    private async Task<Budget> CreateTestBudgetAsync(
        BudgetType type, 
        decimal amount, 
        int year, 
        int month, 
        Guid? categoryId = null)
    {
        var dto = new BudgetCreateDto(
            Type: type,
            Amount: amount,
            Year: year,
            Month: month,
            Description: null,
            CategoryId: categoryId
        );

        var budgetDto = await _budgetService.CreateBudgetAsync(dto, _userId);
        
        return await _context.Budgets.FindAsync(budgetDto.Id) 
            ?? throw new InvalidOperationException("Budget not found");
    }

    #region Full Workflow E2E Tests

    [Fact]
    public async Task FullBudgetWorkflow_CreateBudget_AddExpense_TriggerAlerts()
    {
        // Arrange: 初始化数据
        var year = 2026;
        var month = 4;
        var account = await CreateTestAccountAsync("现金", 10000);
        var foodCategory = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);
        var transportCategory = await CreateTestCategoryAsync("交通", CategoryType.Expense);

        // Step 1: 创建预算
        await CreateTestBudgetAsync(BudgetType.Total, 5000, year, month);
        await CreateTestBudgetAsync(BudgetType.ByCategory, 2000, year, month, foodCategory.Id);
        await CreateTestBudgetAsync(BudgetType.ByCategory, 1000, year, month, transportCategory.Id);

        // Step 2: 验证初始预算状态
        var initialStatus = await _budgetService.GetBudgetStatusAsync(_userId, year, month);
        initialStatus.TotalBudget.Should().Be(5000);
        initialStatus.TotalSpent.Should().Be(0);
        initialStatus.AlertLevel.Should().Be(BudgetAlertLevel.Normal);

        var initialAlerts = await _budgetService.GetBudgetAlertsAsync(_userId, year, month);
        initialAlerts.OverallAlertLevel.Should().Be(BudgetAlertLevel.Normal);
        initialAlerts.CategoryAlerts.Should().BeEmpty();

        // Step 3: 添加第一笔支出（正常范围）
        var transaction1 = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: 1500,
            TransactionDate: new DateTime(year, month, 5, 12, 0, 0, DateTimeKind.Utc),
            Description: "午餐",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: foodCategory.Id,
            TransferToAccountId: null
        );
        
        var result1 = await _transactionService.CreateTransactionWithBudgetCheckAsync(transaction1, _userId);
        
        // 验证第一笔交易后的预警状态
        result1.Transaction.Should().NotBeNull();
        result1.BudgetAlert.Should().NotBeNull();
        result1.BudgetAlert!.OverallAlertLevel.Should().Be(BudgetAlertLevel.Normal);

        var status1 = await _budgetService.GetBudgetStatusAsync(_userId, year, month);
        status1.TotalSpent.Should().Be(1500);
        status1.Percentage.Should().Be(0.3m);
        
        var foodSpending1 = status1.CategorySpending.First(c => c.CategoryName == "餐饮");
        foodSpending1.SpentAmount.Should().Be(1500);
        foodSpending1.Percentage.Should().Be(0.75m);
        foodSpending1.AlertLevel.Should().Be(BudgetAlertLevel.Normal);

        // Step 4: 添加第二笔支出（触发餐饮分类预警）
        var transaction2 = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: 300,
            TransactionDate: new DateTime(year, month, 10, 18, 0, 0, DateTimeKind.Utc),
            Description: "晚餐",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: foodCategory.Id,
            TransferToAccountId: null
        );
        
        var result2 = await _transactionService.CreateTransactionWithBudgetCheckAsync(transaction2, _userId);
        
        // 验证第二笔交易后的预警状态（餐饮达到90%，触发Warning）
        result2.BudgetAlert.Should().NotBeNull();
        result2.BudgetAlert!.OverallAlertLevel.Should().Be(BudgetAlertLevel.Warning);
        result2.BudgetAlert.CategoryAlerts.Should().HaveCount(1);
        result2.BudgetAlert.CategoryAlerts.First().CategoryName.Should().Be("餐饮");
        result2.BudgetAlert.CategoryAlerts.First().Message.Should().Contain("提醒");
        result2.BudgetAlert.CategoryAlerts.First().Percentage.Should().Be(0.9m);

        var status2 = await _budgetService.GetBudgetStatusAsync(_userId, year, month);
        var foodSpending2 = status2.CategorySpending.First(c => c.CategoryName == "餐饮");
        foodSpending2.SpentAmount.Should().Be(1800);
        foodSpending2.Percentage.Should().Be(0.9m);
        foodSpending2.AlertLevel.Should().Be(BudgetAlertLevel.Warning);

        // Step 5: 添加第三笔支出（触发餐饮分类超支告警）
        var transaction3 = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: 500,
            TransactionDate: new DateTime(year, month, 15, 12, 0, 0, DateTimeKind.Utc),
            Description: "聚餐",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: foodCategory.Id,
            TransferToAccountId: null
        );
        
        var result3 = await _transactionService.CreateTransactionWithBudgetCheckAsync(transaction3, _userId);
        
        // 验证第三笔交易后的预警状态（餐饮超支，触发Critical）
        result3.BudgetAlert.Should().NotBeNull();
        result3.BudgetAlert!.OverallAlertLevel.Should().Be(BudgetAlertLevel.Critical);
        result3.BudgetAlert.CategoryAlerts.Should().HaveCount(1);
        result3.BudgetAlert.CategoryAlerts.First().AlertLevel.Should().Be(BudgetAlertLevel.Critical);
        result3.BudgetAlert.CategoryAlerts.First().Message.Should().Contain("告警");
        result3.BudgetAlert.CategoryAlerts.First().Message.Should().Contain("超支");

        var status3 = await _budgetService.GetBudgetStatusAsync(_userId, year, month);
        var foodSpending3 = status3.CategorySpending.First(c => c.CategoryName == "餐饮");
        foodSpending3.SpentAmount.Should().Be(2300);
        foodSpending3.Percentage.Should().Be(1.15m);
        foodSpending3.AlertLevel.Should().Be(BudgetAlertLevel.Critical);
        foodSpending3.IsOverBudget.Should().BeTrue();
        foodSpending3.Remaining.Should().Be(-300);

        // Step 6: 删除一笔支出，验证预警状态变化
        var deleteResult = await _transactionService.DeleteTransactionWithBudgetCheckAsync(result3.Transaction.Id, _userId);
        
        // 验证删除后的预警状态
        deleteResult.Should().NotBeNull();
        deleteResult!.OverallAlertLevel.Should().Be(BudgetAlertLevel.Warning);
        
        var status4 = await _budgetService.GetBudgetStatusAsync(_userId, year, month);
        var foodSpending4 = status4.CategorySpending.First(c => c.CategoryName == "餐饮");
        foodSpending4.SpentAmount.Should().Be(1800);
        foodSpending4.Percentage.Should().Be(0.9m);
        foodSpending4.AlertLevel.Should().Be(BudgetAlertLevel.Warning);
    }

    #endregion

    #region Cross-Month E2E Tests

    [Fact]
    public async Task CrossMonthWorkflow_MonthEndTransactions_ShouldBeInCorrectMonth()
    {
        // Arrange
        var account = await CreateTestAccountAsync("现金", 10000);
        var category = await CreateTestCategoryAsync("消费", CategoryType.Expense);

        // 3月预算
        await CreateTestBudgetAsync(BudgetType.Total, 3000, 2026, 3);
        
        // 4月预算
        await CreateTestBudgetAsync(BudgetType.Total, 4000, 2026, 4);

        // 3月31日的交易（UTC时间）
        var march31Transaction = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: 500,
            TransactionDate: new DateTime(2026, 3, 31, 20, 0, 0, DateTimeKind.Utc),
            Description: "3月末消费",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: category.Id,
            TransferToAccountId: null
        );
        await _transactionService.CreateTransactionAsync(march31Transaction, _userId);

        // 4月1日的交易（UTC时间）
        var april1Transaction = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: 600,
            TransactionDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Description: "4月初消费",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: category.Id,
            TransferToAccountId: null
        );
        await _transactionService.CreateTransactionAsync(april1Transaction, _userId);

        // Act & Assert: UTC时区
        var marchStatusUtc = await _budgetService.GetBudgetStatusAsync(_userId, 2026, 3, TimeZoneInfo.Utc);
        marchStatusUtc.TotalSpent.Should().Be(500);

        var aprilStatusUtc = await _budgetService.GetBudgetStatusAsync(_userId, 2026, 4, TimeZoneInfo.Utc);
        aprilStatusUtc.TotalSpent.Should().Be(600);

        // Act & Assert: 北京时间（UTC+8）
        // 注意：3月31日20:00 UTC = 4月1日04:00 北京时间
        var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        
        var marchStatusCn = await _budgetService.GetBudgetStatusAsync(_userId, 2026, 3, chinaTimeZone);
        marchStatusCn.TotalSpent.Should().Be(0);

        var aprilStatusCn = await _budgetService.GetBudgetStatusAsync(_userId, 2026, 4, chinaTimeZone);
        aprilStatusCn.TotalSpent.Should().Be(1100);
    }

    #endregion

    #region Time Zone Boundary E2E Tests

    [Fact]
    public async Task TimeZoneBoundary_NewYearEve_ShouldBeInCorrectYear()
    {
        // Arrange
        var account = await CreateTestAccountAsync("现金", 10000);
        var category = await CreateTestCategoryAsync("消费", CategoryType.Expense);

        // 2026年12月预算
        await CreateTestBudgetAsync(BudgetType.Total, 10000, 2026, 12);
        
        // 2027年1月预算
        await CreateTestBudgetAsync(BudgetType.Total, 10000, 2027, 1);

        // 2026年12月31日 20:00 UTC = 2027年1月1日 04:00 北京时间
        var transaction = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: 1000,
            TransactionDate: new DateTime(2026, 12, 31, 20, 0, 0, DateTimeKind.Utc),
            Description: "跨年消费",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: category.Id,
            TransferToAccountId: null
        );
        await _transactionService.CreateTransactionAsync(transaction, _userId);

        // Act & Assert: UTC时区
        var dec2026StatusUtc = await _budgetService.GetBudgetStatusAsync(_userId, 2026, 12, TimeZoneInfo.Utc);
        dec2026StatusUtc.TotalSpent.Should().Be(1000);

        var jan2027StatusUtc = await _budgetService.GetBudgetStatusAsync(_userId, 2027, 1, TimeZoneInfo.Utc);
        jan2027StatusUtc.TotalSpent.Should().Be(0);

        // Act & Assert: 北京时间
        var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        
        var dec2026StatusCn = await _budgetService.GetBudgetStatusAsync(_userId, 2026, 12, chinaTimeZone);
        dec2026StatusCn.TotalSpent.Should().Be(0);

        var jan2027StatusCn = await _budgetService.GetBudgetStatusAsync(_userId, 2027, 1, chinaTimeZone);
        jan2027StatusCn.TotalSpent.Should().Be(1000);
    }

    #endregion

    #region Budget Card Data E2E Tests

    [Fact]
    public async Task BudgetCardData_ShouldContainAllRequiredFields()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var category = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);

        await CreateTestBudgetAsync(BudgetType.Total, 5000, year, month);
        await CreateTestBudgetAsync(BudgetType.ByCategory, 2000, year, month, category.Id);

        var account = await CreateTestAccountAsync("现金", 10000);
        
        var transaction = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: 3500,
            TransactionDate: new DateTime(year, month, 15, 12, 0, 0, DateTimeKind.Utc),
            Description: "大额消费",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: category.Id,
            TransferToAccountId: null
        );
        await _transactionService.CreateTransactionAsync(transaction, _userId);

        // Act
        var status = await _budgetService.GetBudgetStatusAsync(_userId, year, month);
        var alerts = await _budgetService.GetBudgetAlertsAsync(_userId, year, month);

        // Assert: 预算卡片所需字段
        status.TotalBudget.Should().Be(5000);
        status.TotalSpent.Should().Be(3500);
        status.Remaining.Should().Be(1500);
        status.Percentage.Should().Be(0.7m);
        status.AlertLevel.Should().Be(BudgetAlertLevel.Normal);
        status.IsOverBudget.Should().BeFalse();

        // 分类预算卡片数据
        var categorySpending = status.CategorySpending.First(c => c.CategoryName == "餐饮");
        categorySpending.BudgetAmount.Should().Be(2000);
        categorySpending.SpentAmount.Should().Be(3500);
        categorySpending.Remaining.Should().Be(-1500);
        categorySpending.Percentage.Should().Be(1.75m);
        categorySpending.AlertLevel.Should().Be(BudgetAlertLevel.Critical);
        categorySpending.IsOverBudget.Should().BeTrue();

        // 预警信息
        alerts.OverallAlertLevel.Should().Be(BudgetAlertLevel.Critical);
        alerts.CategoryAlerts.Should().HaveCount(1);
        alerts.CategoryAlerts.First().Message.Should().Contain("超支");
        alerts.CategoryAlerts.First().Percentage.Should().Be(1.75m);
    }

    #endregion

    #region Update Transaction E2E Tests

    [Fact]
    public async Task UpdateTransaction_ChangeAmount_ShouldUpdateBudgetAlerts()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var account = await CreateTestAccountAsync("现金", 10000);
        var category = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);

        await CreateTestBudgetAsync(BudgetType.ByCategory, 1000, year, month, category.Id);

        // 创建初始交易（500元，50%预算）
        var createDto = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: 500,
            TransactionDate: new DateTime(year, month, 10, 12, 0, 0, DateTimeKind.Utc),
            Description: "午餐",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: category.Id,
            TransferToAccountId: null
        );
        
        var createdResult = await _transactionService.CreateTransactionWithBudgetCheckAsync(createDto, _userId);
        createdResult.BudgetAlert.Should().NotBeNull();
        createdResult.BudgetAlert!.OverallAlertLevel.Should().Be(BudgetAlertLevel.Normal);

        // Act: 更新交易金额为900元（90%预算，触发Warning）
        var updateDto = new TransactionUpdateDto(
            Type: TransactionType.Expense,
            Amount: 900,
            TransactionDate: new DateTime(year, month, 10, 12, 0, 0, DateTimeKind.Utc),
            Description: "午餐（更新）",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: category.Id,
            TransferToAccountId: null
        );

        var updateResult = await _transactionService.UpdateTransactionWithBudgetCheckAsync(
            createdResult.Transaction.Id, 
            updateDto, 
            _userId);

        // Assert
        updateResult.Transaction.Amount.Should().Be(900);
        updateResult.BudgetAlert.Should().NotBeNull();
        updateResult.BudgetAlert!.OverallAlertLevel.Should().Be(BudgetAlertLevel.Warning);
        updateResult.BudgetAlert.CategoryAlerts.Should().HaveCount(1);
        updateResult.BudgetAlert.CategoryAlerts.First().Percentage.Should().Be(0.9m);

        // 验证预算状态
        var status = await _budgetService.GetBudgetStatusAsync(_userId, year, month);
        var categorySpending = status.CategorySpending.First();
        categorySpending.SpentAmount.Should().Be(900);
        categorySpending.Percentage.Should().Be(0.9m);
        categorySpending.AlertLevel.Should().Be(BudgetAlertLevel.Warning);
    }

    #endregion
}
