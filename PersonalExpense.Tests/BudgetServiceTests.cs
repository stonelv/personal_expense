using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Tests;

public class BudgetServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ApplicationDbContext _context;
    private readonly BudgetService _service;

    public BudgetServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new BudgetService(_context);
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
        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            Type = type,
            Amount = amount,
            Year = year,
            Month = month,
            CreatedAt = DateTime.UtcNow,
            UserId = _userId,
            CategoryId = categoryId
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();
        return budget;
    }

    private async Task<Transaction> CreateTestTransactionAsync(
        TransactionType type,
        decimal amount,
        DateTime transactionDate,
        Guid? categoryId = null)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "Test Account",
            Type = AccountType.Cash,
            Balance = 10000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserId = _userId
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Type = type,
            Amount = amount,
            TransactionDate = transactionDate,
            CreatedAt = DateTime.UtcNow,
            UserId = _userId,
            AccountId = account.Id,
            CategoryId = categoryId
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    #region Budget Status Calculation Tests

    [Fact]
    public async Task GetBudgetStatusAsync_WithNoSpending_ShouldReturnZeroPercentage()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.TotalBudget.Should().Be(1000);
        result.TotalSpent.Should().Be(0);
        result.Percentage.Should().Be(0);
        result.AlertLevel.Should().Be(BudgetAlertLevel.Normal);
        result.IsOverBudget.Should().BeFalse();
    }

    [Fact]
    public async Task GetBudgetStatusAsync_WithNormalSpending_ShouldReturnNormalAlert()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);
        
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 500, transactionDate);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.TotalSpent.Should().Be(500);
        result.Percentage.Should().Be(0.5m);
        result.AlertLevel.Should().Be(BudgetAlertLevel.Normal);
    }

    [Fact]
    public async Task GetBudgetStatusAsync_With80PercentSpending_ShouldReturnWarningAlert()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);
        
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 850, transactionDate);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.TotalSpent.Should().Be(850);
        result.Percentage.Should().Be(0.85m);
        result.AlertLevel.Should().Be(BudgetAlertLevel.Warning);
        result.IsOverBudget.Should().BeFalse();
    }

    [Fact]
    public async Task GetBudgetStatusAsync_WithExact80PercentSpending_ShouldReturnWarningAlert()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);
        
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 800, transactionDate);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.Percentage.Should().Be(0.8m);
        result.AlertLevel.Should().Be(BudgetAlertLevel.Warning);
    }

    [Fact]
    public async Task GetBudgetStatusAsync_WithOverBudget_ShouldReturnCriticalAlert()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);
        
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 1200, transactionDate);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.TotalSpent.Should().Be(1200);
        result.Percentage.Should().Be(1.2m);
        result.AlertLevel.Should().Be(BudgetAlertLevel.Critical);
        result.IsOverBudget.Should().BeTrue();
        result.Remaining.Should().Be(-200);
    }

    [Fact]
    public async Task GetBudgetStatusAsync_WithExactBudget_ShouldReturnCriticalAlert()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);
        
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 1000, transactionDate);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.Percentage.Should().Be(1.0m);
        result.AlertLevel.Should().Be(BudgetAlertLevel.Critical);
        result.IsOverBudget.Should().BeTrue();
    }

    #endregion

    #region Category Budget Tests

    [Fact]
    public async Task GetBudgetStatusAsync_WithCategoryBudget_ShouldCalculateCategorySpending()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var foodCategory = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);
        var transportCategory = await CreateTestCategoryAsync("交通", CategoryType.Expense);

        await CreateTestBudgetAsync(BudgetType.ByCategory, 500, year, month, foodCategory.Id);
        await CreateTestBudgetAsync(BudgetType.ByCategory, 300, year, month, transportCategory.Id);

        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 400, transactionDate, foodCategory.Id);
        await CreateTestTransactionAsync(TransactionType.Expense, 100, transactionDate, transportCategory.Id);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.CategorySpending.Should().HaveCount(2);

        var foodSpending = result.CategorySpending.First(c => c.CategoryName == "餐饮");
        foodSpending.BudgetAmount.Should().Be(500);
        foodSpending.SpentAmount.Should().Be(400);
        foodSpending.Percentage.Should().Be(0.8m);
        foodSpending.AlertLevel.Should().Be(BudgetAlertLevel.Warning);

        var transportSpending = result.CategorySpending.First(c => c.CategoryName == "交通");
        transportSpending.BudgetAmount.Should().Be(300);
        transportSpending.SpentAmount.Should().Be(100);
        transportSpending.Percentage.Should().BeApproximately(0.333m, 0.001m);
        transportSpending.AlertLevel.Should().Be(BudgetAlertLevel.Normal);
    }

    [Fact]
    public async Task GetBudgetStatusAsync_WithCategoryOverBudget_ShouldReturnCriticalAlert()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var foodCategory = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);

        await CreateTestBudgetAsync(BudgetType.ByCategory, 500, year, month, foodCategory.Id);

        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 600, transactionDate, foodCategory.Id);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        var foodSpending = result.CategorySpending.First(c => c.CategoryName == "餐饮");
        foodSpending.Percentage.Should().Be(1.2m);
        foodSpending.AlertLevel.Should().Be(BudgetAlertLevel.Critical);
        foodSpending.IsOverBudget.Should().BeTrue();
    }

    #endregion

    #region Budget Alerts Tests

    [Fact]
    public async Task GetBudgetAlertsAsync_WithNormalSpending_ShouldReturnNoAlerts()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);
        
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 500, transactionDate);

        // Act
        var result = await _service.GetBudgetAlertsAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.OverallAlertLevel.Should().Be(BudgetAlertLevel.Normal);
        result.OverallMessage.Should().BeNull();
        result.CategoryAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBudgetAlertsAsync_WithWarningSpending_ShouldReturnWarningMessage()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);
        
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 850, transactionDate);

        // Act
        var result = await _service.GetBudgetAlertsAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.OverallAlertLevel.Should().Be(BudgetAlertLevel.Warning);
        result.OverallMessage.Should().Contain("提醒");
        result.OverallMessage.Should().Contain("85");
    }

    [Fact]
    public async Task GetBudgetAlertsAsync_WithCriticalSpending_ShouldReturnCriticalMessage()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);
        
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 1200, transactionDate);

        // Act
        var result = await _service.GetBudgetAlertsAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.OverallAlertLevel.Should().Be(BudgetAlertLevel.Critical);
        result.OverallMessage.Should().Contain("告警");
        result.OverallMessage.Should().Contain("120");
        result.OverallMessage.Should().Contain("超支");
    }

    [Fact]
    public async Task GetBudgetAlertsAsync_WithCategoryWarning_ShouldReturnCategoryAlert()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var foodCategory = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);

        await CreateTestBudgetAsync(BudgetType.ByCategory, 500, year, month, foodCategory.Id);

        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 450, transactionDate, foodCategory.Id);

        // Act
        var result = await _service.GetBudgetAlertsAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.OverallAlertLevel.Should().Be(BudgetAlertLevel.Warning);
        result.CategoryAlerts.Should().HaveCount(1);
        
        var categoryAlert = result.CategoryAlerts.First();
        categoryAlert.CategoryName.Should().Be("餐饮");
        categoryAlert.AlertLevel.Should().Be(BudgetAlertLevel.Warning);
        categoryAlert.Message.Should().Contain("餐饮");
        categoryAlert.Message.Should().Contain("90");
    }

    [Fact]
    public async Task GetBudgetAlertsAsync_WithMultipleCategoryAlerts_ShouldReturnHighestAlertLevel()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var foodCategory = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);
        var transportCategory = await CreateTestCategoryAsync("交通", CategoryType.Expense);

        await CreateTestBudgetAsync(BudgetType.ByCategory, 500, year, month, foodCategory.Id);
        await CreateTestBudgetAsync(BudgetType.ByCategory, 300, year, month, transportCategory.Id);

        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 600, transactionDate, foodCategory.Id);
        await CreateTestTransactionAsync(TransactionType.Expense, 250, transactionDate, transportCategory.Id);

        // Act
        var result = await _service.GetBudgetAlertsAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.OverallAlertLevel.Should().Be(BudgetAlertLevel.Critical);
        result.CategoryAlerts.Should().HaveCount(2);
    }

    #endregion

    #region Cross-Month Tests

    [Fact]
    public async Task GetBudgetStatusAsync_WithCrossMonthTransactions_ShouldOnlyIncludeCurrentMonth()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);

        var marchTransaction = new DateTime(year, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 300, marchTransaction);

        var aprilTransaction = new DateTime(year, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 400, aprilTransaction);

        var mayTransaction = new DateTime(year, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 500, mayTransaction);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.TotalSpent.Should().Be(400);
        result.Percentage.Should().Be(0.4m);
    }

    #endregion

    #region Time Zone Tests

    [Fact]
    public async Task GetBudgetStatusAsync_WithTimeZone_ShouldConvertDatesCorrectly()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);

        var utcTime = new DateTime(year, 3, 31, 20, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 300, utcTime);

        var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, 4, chinaTimeZone);

        // Assert
        result.Should().NotBeNull();
        result.TotalSpent.Should().Be(300);
    }

    [Fact]
    public async Task GetBudgetStatusAsync_WithTimeZoneBoundary_ShouldHandleCorrectly()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);

        var utcTime1 = new DateTime(year, 3, 31, 16, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 300, utcTime1);

        var utcTime2 = new DateTime(year, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 400, utcTime2);

        var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, 4, chinaTimeZone);

        // Assert
        result.Should().NotBeNull();
        result.TotalSpent.Should().Be(700);
    }

    #endregion

    #region Budget Duplicate Check Tests

    [Fact]
    public async Task CreateBudgetAsync_WithDuplicateTotalBudget_ShouldThrowException()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);

        var dto = new BudgetCreateDto(
            Type: BudgetType.Total,
            Amount: 2000,
            Year: year,
            Month: month,
            Description: "Duplicate",
            CategoryId: null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateBudgetAsync(dto, _userId));
        
        exception.Message.Should().Contain("已存在相同类型的预算");
    }

    [Fact]
    public async Task CreateBudgetAsync_WithDuplicateCategoryBudget_ShouldThrowException()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var category = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);
        await CreateTestBudgetAsync(BudgetType.ByCategory, 500, year, month, category.Id);

        var dto = new BudgetCreateDto(
            Type: BudgetType.ByCategory,
            Amount: 800,
            Year: year,
            Month: month,
            Description: "Duplicate",
            CategoryId: category.Id
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateBudgetAsync(dto, _userId));
        
        exception.Message.Should().Contain("已存在相同类型的预算");
    }

    [Fact]
    public async Task CreateBudgetAsync_WithDifferentCategory_ShouldSucceed()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var foodCategory = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);
        var transportCategory = await CreateTestCategoryAsync("交通", CategoryType.Expense);
        
        await CreateTestBudgetAsync(BudgetType.ByCategory, 500, year, month, foodCategory.Id);

        var dto = new BudgetCreateDto(
            Type: BudgetType.ByCategory,
            Amount: 300,
            Year: year,
            Month: month,
            Description: "Transport Budget",
            CategoryId: transportCategory.Id
        );

        // Act
        var result = await _service.CreateBudgetAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.CategoryId.Should().Be(transportCategory.Id);
    }

    #endregion

    #region Transaction Budget Alert Tests

    [Fact]
    public async Task CheckBudgetAlertAfterTransactionAsync_WithNonExpense_ShouldReturnNull()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);

        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await _service.CheckBudgetAlertAfterTransactionAsync(
            _userId, 
            TransactionType.Income, 
            transactionDate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckBudgetAlertAfterTransactionAsync_WithNoBudget_ShouldReturnNull()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await _service.CheckBudgetAlertAfterTransactionAsync(
            _userId, 
            TransactionType.Expense, 
            transactionDate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckBudgetAlertAfterTransactionAsync_WithBudget_ShouldReturnAlerts()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);
        
        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 850, transactionDate);

        // Act
        var result = await _service.CheckBudgetAlertAfterTransactionAsync(
            _userId, 
            TransactionType.Expense, 
            transactionDate);

        // Assert
        result.Should().NotBeNull();
        result!.OverallAlertLevel.Should().Be(BudgetAlertLevel.Warning);
    }

    #endregion

    #region Uncategorized Spending Tests

    [Fact]
    public async Task GetBudgetStatusAsync_WithUncategorizedSpending_ShouldShowAsUncategorized()
    {
        // Arrange
        var year = 2026;
        var month = 4;
        var foodCategory = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);
        
        await CreateTestBudgetAsync(BudgetType.ByCategory, 500, year, month, foodCategory.Id);
        await CreateTestBudgetAsync(BudgetType.Total, 1000, year, month);

        var transactionDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);
        await CreateTestTransactionAsync(TransactionType.Expense, 300, transactionDate, foodCategory.Id);
        await CreateTestTransactionAsync(TransactionType.Expense, 200, transactionDate, null);

        // Act
        var result = await _service.GetBudgetStatusAsync(_userId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.TotalSpent.Should().Be(500);
        
        var uncategorized = result.CategorySpending.FirstOrDefault(c => c.CategoryName == "未分类");
        uncategorized.Should().NotBeNull();
        uncategorized!.SpentAmount.Should().Be(200);
        uncategorized.BudgetAmount.Should().Be(0);
    }

    #endregion
}
