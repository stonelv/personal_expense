using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;
using System.Text;

namespace PersonalExpense.Tests;

public class TransactionServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ApplicationDbContext _context;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new TransactionService(_context);
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

    #region Income Tests

    [Fact]
    public async Task CreateTransaction_Income_ShouldIncreaseAccountBalance()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;
        var incomeAmount = 500;

        var dto = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: incomeAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Salary",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        // Act
        var result = await _service.CreateTransactionAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(TransactionType.Income);
        result.Amount.Should().Be(incomeAmount);

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance + incomeAmount);
    }

    [Fact]
    public async Task UpdateTransaction_Income_ShouldAdjustAccountBalance()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;
        var originalIncome = 500;
        var newIncome = 800;

        var createDto = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: originalIncome,
            TransactionDate: DateTime.UtcNow,
            Description: "Salary",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        var createdTransaction = await _service.CreateTransactionAsync(createDto, _userId);

        var updateDto = new TransactionUpdateDto(
            Type: TransactionType.Income,
            Amount: newIncome,
            TransactionDate: DateTime.UtcNow,
            Description: "Updated Salary",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        // Act
        var result = await _service.UpdateTransactionAsync(createdTransaction.Id, updateDto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(newIncome);

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance + newIncome);
    }

    [Fact]
    public async Task DeleteTransaction_Income_ShouldDecreaseAccountBalance()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;
        var incomeAmount = 500;

        var createDto = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: incomeAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Salary",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        var createdTransaction = await _service.CreateTransactionAsync(createDto, _userId);

        // Act
        await _service.DeleteTransactionAsync(createdTransaction.Id, _userId);

        // Assert
        var deletedTransaction = await _context.Transactions.FindAsync(createdTransaction.Id);
        deletedTransaction.Should().BeNull();

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance);
    }

    #endregion

    #region Expense Tests

    [Fact]
    public async Task CreateTransaction_Expense_ShouldDecreaseAccountBalance()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;
        var expenseAmount = 200;

        var dto = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: expenseAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Groceries",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        // Act
        var result = await _service.CreateTransactionAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(TransactionType.Expense);
        result.Amount.Should().Be(expenseAmount);

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance - expenseAmount);
    }

    [Fact]
    public async Task DeleteTransaction_Expense_ShouldIncreaseAccountBalance()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;
        var expenseAmount = 200;

        var createDto = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: expenseAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Groceries",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        var createdTransaction = await _service.CreateTransactionAsync(createDto, _userId);

        // Act
        await _service.DeleteTransactionAsync(createdTransaction.Id, _userId);

        // Assert
        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance);
    }

    #endregion

    #region Transfer Tests

    [Fact]
    public async Task CreateTransaction_Transfer_ShouldTransferBetweenAccounts()
    {
        // Arrange
        var fromAccount = await CreateTestAccountAsync("Cash", 1000);
        var toAccount = await CreateTestAccountAsync("Savings", 500);
        var fromInitial = fromAccount.Balance;
        var toInitial = toAccount.Balance;
        var transferAmount = 300;

        var dto = new TransactionCreateDto(
            Type: TransactionType.Transfer,
            Amount: transferAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Transfer to savings",
            AttachmentUrl: null,
            AccountId: fromAccount.Id,
            CategoryId: null,
            TransferToAccountId: toAccount.Id
        );

        // Act
        var result = await _service.CreateTransactionAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(TransactionType.Transfer);
        result.Amount.Should().Be(transferAmount);

        var updatedFromAccount = await _context.Accounts.FindAsync(fromAccount.Id);
        var updatedToAccount = await _context.Accounts.FindAsync(toAccount.Id);

        updatedFromAccount.Should().NotBeNull();
        updatedToAccount.Should().NotBeNull();
        updatedFromAccount!.Balance.Should().Be(fromInitial - transferAmount);
        updatedToAccount!.Balance.Should().Be(toInitial + transferAmount);
    }

    [Fact]
    public async Task DeleteTransaction_Transfer_ShouldReverseTransfer()
    {
        // Arrange
        var fromAccount = await CreateTestAccountAsync("Cash", 1000);
        var toAccount = await CreateTestAccountAsync("Savings", 500);
        var fromInitial = fromAccount.Balance;
        var toInitial = toAccount.Balance;
        var transferAmount = 300;

        var createDto = new TransactionCreateDto(
            Type: TransactionType.Transfer,
            Amount: transferAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Transfer to savings",
            AttachmentUrl: null,
            AccountId: fromAccount.Id,
            CategoryId: null,
            TransferToAccountId: toAccount.Id
        );

        var createdTransaction = await _service.CreateTransactionAsync(createDto, _userId);

        // Act
        await _service.DeleteTransactionAsync(createdTransaction.Id, _userId);

        // Assert
        var updatedFromAccount = await _context.Accounts.FindAsync(fromAccount.Id);
        var updatedToAccount = await _context.Accounts.FindAsync(toAccount.Id);

        updatedFromAccount.Should().NotBeNull();
        updatedToAccount.Should().NotBeNull();
        updatedFromAccount!.Balance.Should().Be(fromInitial);
        updatedToAccount!.Balance.Should().Be(toInitial);
    }

    #endregion

    #region Transaction Rollback Tests

    [Fact]
    public async Task CreateTransaction_WithInvalidAccount_ShouldNotCreateTransaction()
    {
        // Arrange
        var invalidAccountId = Guid.NewGuid();
        var dto = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: 500,
            TransactionDate: DateTime.UtcNow,
            Description: "Test",
            AttachmentUrl: null,
            AccountId: invalidAccountId,
            CategoryId: null,
            TransferToAccountId: null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateTransactionAsync(dto, _userId));

        exception.Message.Should().Contain("Account not found");

        var transactions = await _context.Transactions.ToListAsync();
        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTransfer_WithInvalidToAccount_ShouldNotCreateTransaction()
    {
        // Arrange
        var fromAccount = await CreateTestAccountAsync("Cash", 1000);
        var fromInitial = fromAccount.Balance;
        var invalidToAccountId = Guid.NewGuid();

        var dto = new TransactionCreateDto(
            Type: TransactionType.Transfer,
            Amount: 300,
            TransactionDate: DateTime.UtcNow,
            Description: "Test Transfer",
            AttachmentUrl: null,
            AccountId: fromAccount.Id,
            CategoryId: null,
            TransferToAccountId: invalidToAccountId
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateTransactionAsync(dto, _userId));

        exception.Message.Should().Contain("Transfer to account not found");

        var transactions = await _context.Transactions.ToListAsync();
        transactions.Should().BeEmpty();

        var updatedFromAccount = await _context.Accounts.FindAsync(fromAccount.Id);
        updatedFromAccount.Should().NotBeNull();
        updatedFromAccount!.Balance.Should().Be(fromInitial);
    }

    [Fact]
    public async Task UpdateTransaction_WithInvalidAccount_ShouldNotModifyTransaction()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;

        var createDto = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: 500,
            TransactionDate: DateTime.UtcNow,
            Description: "Salary",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        var createdTransaction = await _service.CreateTransactionAsync(createDto, _userId);

        var invalidAccountId = Guid.NewGuid();
        var updateDto = new TransactionUpdateDto(
            Type: TransactionType.Income,
            Amount: 800,
            TransactionDate: DateTime.UtcNow,
            Description: "Updated",
            AttachmentUrl: null,
            AccountId: invalidAccountId,
            CategoryId: null,
            TransferToAccountId: null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateTransactionAsync(createdTransaction.Id, updateDto, _userId));

        exception.Message.Should().Contain("Account not found");

        var transaction = await _context.Transactions.FindAsync(createdTransaction.Id);
        transaction.Should().NotBeNull();
        transaction!.Amount.Should().Be(500);

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance + 500);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task CreateTransaction_TransferToSameAccount_ShouldSucceedWithNoBalanceChange()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;

        var dto = new TransactionCreateDto(
            Type: TransactionType.Transfer,
            Amount: 300,
            TransactionDate: DateTime.UtcNow,
            Description: "Transfer to self",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: account.Id
        );

        // Act
        var result = await _service.CreateTransactionAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        
        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance);
    }

    [Fact]
    public async Task CreateTransaction_WithZeroAmount_ShouldSucceed()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;

        var dto = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: 0,
            TransactionDate: DateTime.UtcNow,
            Description: "Zero amount",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        // Act
        var result = await _service.CreateTransactionAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(0);

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance);
    }

    [Fact]
    public async Task UpdateTransaction_ChangeTypeFromIncomeToExpense_ShouldAdjustBalanceCorrectly()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;
        var originalAmount = 500;

        var createDto = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: originalAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Income",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        var createdTransaction = await _service.CreateTransactionAsync(createDto, _userId);

        var updateDto = new TransactionUpdateDto(
            Type: TransactionType.Expense,
            Amount: originalAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Changed to Expense",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );

        // Act
        var result = await _service.UpdateTransactionAsync(createdTransaction.Id, updateDto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(TransactionType.Expense);

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance - originalAmount);
    }

    [Fact]
    public async Task GetTransactions_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        
        for (int i = 0; i < 25; i++)
        {
            var dto = new TransactionCreateDto(
                Type: TransactionType.Income,
                Amount: 100 + i,
                TransactionDate: DateTime.UtcNow.AddDays(-i),
                Description: $"Income {i}",
                AttachmentUrl: null,
                AccountId: account.Id,
                CategoryId: null,
                TransferToAccountId: null
            );
            await _service.CreateTransactionAsync(dto, _userId);
        }

        // Act
        var filter = new TransactionFilterParams
        {
            PageNumber = 2,
            PageSize = 10
        };
        var result = await _service.GetTransactionsAsync(_userId, filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Count.Should().Be(10);
        result.TotalPages.Should().Be(3);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetTransactions_WithTypeFilter_ShouldReturnOnlyMatchingTransactions()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        for (int i = 0; i < 5; i++)
        {
            var incomeDto = new TransactionCreateDto(
                Type: TransactionType.Income,
                Amount: 100,
                TransactionDate: DateTime.UtcNow,
                Description: $"Income {i}",
                AttachmentUrl: null,
                AccountId: account.Id,
                CategoryId: null,
                TransferToAccountId: null
            );
            await _service.CreateTransactionAsync(incomeDto, _userId);

            var expenseDto = new TransactionCreateDto(
                Type: TransactionType.Expense,
                Amount: 50,
                TransactionDate: DateTime.UtcNow,
                Description: $"Expense {i}",
                AttachmentUrl: null,
                AccountId: account.Id,
                CategoryId: null,
                TransferToAccountId: null
            );
            await _service.CreateTransactionAsync(expenseDto, _userId);
        }

        // Act
        var filter = new TransactionFilterParams
        {
            Type = TransactionType.Income,
            PageNumber = 1,
            PageSize = 100
        };
        var result = await _service.GetTransactionsAsync(_userId, filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(5);
        result.Items.All(t => t.Type == TransactionType.Income).Should().BeTrue();
    }

    #endregion

    #region Import Tests

    private static Stream CreateCsvStream(string csvContent)
    {
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        var stream = new MemoryStream(bytes);
        return stream;
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

    [Fact]
    public async Task ImportTransactions_WithValidData_ShouldImportSuccessfully()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;
        await CreateTestCategoryAsync("餐饮", CategoryType.Expense);
        await CreateTestCategoryAsync("交通", CategoryType.Expense);

        var csvContent = @"日期,分类,金额,备注
2026-01-15,餐饮,35.50,午餐
2026-01-16,交通,12.00,地铁
2026-01-17,餐饮,88.80,晚餐";

        using var stream = CreateCsvStream(csvContent);

        // Act
        var result = await _service.ImportTransactionsAsync(stream, _userId, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(3);
        result.AddedCount.Should().Be(3);
        result.SkippedCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
        result.SkippedRecords.Should().BeEmpty();
        result.Errors.Should().BeEmpty();

        var transactions = await _context.Transactions
            .Where(t => t.UserId == _userId)
            .ToListAsync();
        transactions.Count.Should().Be(3);

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance - 35.50m - 12.00m - 88.80m);
    }

    [Theory]
    [InlineData("2026-01-15", "yyyy-MM-dd")]
    [InlineData("2026/01/15", "yyyy/MM/dd")]
    [InlineData("01/15/2026", "MM/dd/yyyy")]
    [InlineData("01-15-2026", "MM-dd-yyyy")]
    [InlineData("15-01-2026", "dd-MM-yyyy")]
    [InlineData("2026年01月15日", "yyyy年MM月dd日")]
    [InlineData("20260115", "yyyyMMdd")]
    public async Task ImportTransactions_WithDifferentDateFormats_ShouldParseCorrectly(string dateValue, string format)
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        await CreateTestCategoryAsync("餐饮", CategoryType.Expense);

        var csvContent = $@"日期,分类,金额,备注
{dateValue},餐饮,35.50,午餐";

        using var stream = CreateCsvStream(csvContent);

        // Act
        var result = await _service.ImportTransactionsAsync(stream, _userId, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.ErrorCount.Should().Be(0);
        result.AddedCount.Should().Be(1);
        result.IsSuccess.Should().BeTrue();

        var transaction = await _context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction!.TransactionDate.Should().Be(new DateTime(2026, 1, 15).Date);
    }

    [Fact]
    public async Task ImportTransactions_WithFileDuplicates_ShouldSkipDuplicates()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        await CreateTestCategoryAsync("餐饮", CategoryType.Expense);

        var csvContent = @"日期,分类,金额,备注
2026-01-15,餐饮,35.50,午餐
2026-01-15,餐饮,35.50,午餐
2026-01-16,餐饮,50.00,晚餐";

        using var stream = CreateCsvStream(csvContent);

        // Act
        var result = await _service.ImportTransactionsAsync(stream, _userId, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(3);
        result.AddedCount.Should().Be(2);
        result.SkippedCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.IsSuccess.Should().BeTrue();

        result.SkippedRecords.Count.Should().Be(1);
        result.SkippedRecords[0].Reason.Should().Be("文件内重复记录");
        result.SkippedRecords[0].RowNumber.Should().Be(3);

        var transactions = await _context.Transactions
            .Where(t => t.UserId == _userId)
            .ToListAsync();
        transactions.Count.Should().Be(2);
    }

    [Fact]
    public async Task ImportTransactions_WithHistoryDuplicates_ShouldSkipDuplicates()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var category = await CreateTestCategoryAsync("餐饮", CategoryType.Expense);

        var existingTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Type = TransactionType.Expense,
            Amount = 35.50m,
            TransactionDate = new DateTime(2026, 1, 15),
            Description = "午餐",
            CreatedAt = DateTime.UtcNow,
            UserId = _userId,
            AccountId = account.Id,
            CategoryId = category.Id
        };
        _context.Transactions.Add(existingTransaction);
        account.Balance -= 35.50m;
        await _context.SaveChangesAsync();

        var initialBalance = account.Balance;

        var csvContent = @"日期,分类,金额,备注
2026-01-15,餐饮,35.50,午餐
2026-01-16,餐饮,50.00,晚餐";

        using var stream = CreateCsvStream(csvContent);

        // Act
        var result = await _service.ImportTransactionsAsync(stream, _userId, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(2);
        result.AddedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.IsSuccess.Should().BeTrue();

        result.SkippedRecords.Count.Should().Be(1);
        result.SkippedRecords[0].Reason.Should().Be("历史重复记录");

        var transactions = await _context.Transactions
            .Where(t => t.UserId == _userId)
            .ToListAsync();
        transactions.Count.Should().Be(2);

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance - 50.00m);
    }

    [Theory]
    [InlineData("日期,分类,备注\n2026-01-15,餐饮,午餐", "金额")]
    [InlineData("日期,金额,备注\n2026-01-15,35.50,午餐", "分类")]
    [InlineData("分类,金额,备注\n餐饮,35.50,午餐", "日期")]
    [InlineData("备注\n午餐", "日期, 分类, 金额")]
    public async Task ImportTransactions_WithMissingHeaders_ShouldReturnError(string csvContent, string missingHeader)
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        using var stream = CreateCsvStream(csvContent);

        // Act
        var result = await _service.ImportTransactionsAsync(stream, _userId, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.ErrorCount.Should().Be(1);
        result.AddedCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.IsSuccess.Should().BeFalse();

        result.Errors.Count.Should().Be(1);
        result.Errors[0].RowNumber.Should().Be(1);
        result.Errors[0].ErrorMessage.Should().Contain("缺少必需的表头列");

        var transactions = await _context.Transactions
            .Where(t => t.UserId == _userId)
            .ToListAsync();
        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportTransactions_WithPartialErrors_ShouldReturnCorrectStats()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;
        await CreateTestCategoryAsync("餐饮", CategoryType.Expense);
        await CreateTestCategoryAsync("交通", CategoryType.Expense);

        var csvContent = @"日期,分类,金额,备注
2026-01-15,餐饮,35.50,午餐
,交通,12.00,地铁
2026-01-17,餐饮,88.80,晚餐
2026-01-18,,50.00,娱乐
2026-01-19,交通,0,无效金额
2026-01-20,餐饮,-10,负数金额
2026-01-21,交通,20.00,公交";

        using var stream = CreateCsvStream(csvContent);

        // Act
        var result = await _service.ImportTransactionsAsync(stream, _userId, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(7);
        result.AddedCount.Should().Be(4);
        result.ErrorCount.Should().Be(3);
        result.SkippedCount.Should().Be(0);
        result.IsSuccess.Should().BeFalse();

        result.Errors.Count.Should().Be(3);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("日期不能为空"));
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("分类不能为空"));
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("金额必须是大于0的数字"));

        var transactions = await _context.Transactions
            .Where(t => t.UserId == _userId)
            .ToListAsync();
        transactions.Count.Should().Be(4);

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialBalance - 35.50m - 88.80m - 20.00m);
    }

    [Fact]
    public async Task ImportTransactions_WithNewCategories_ShouldCreateCategories()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        await CreateTestCategoryAsync("餐饮", CategoryType.Expense);

        var csvContent = @"日期,分类,金额,备注
2026-01-15,餐饮,35.50,午餐
2026-01-16,新分类1,50.00,测试新分类
2026-01-17,新分类2,100.00,另一个新分类
2026-01-18,新分类1,25.00,重复新分类";

        using var stream = CreateCsvStream(csvContent);

        // Act
        var result = await _service.ImportTransactionsAsync(stream, _userId, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.ErrorCount.Should().Be(0);
        result.AddedCount.Should().Be(4);
        result.IsSuccess.Should().BeTrue();

        var categories = await _context.Categories
            .Where(c => c.UserId == _userId)
            .ToListAsync();
        categories.Count.Should().Be(3);
        categories.Should().Contain(c => c.Name == "餐饮");
        categories.Should().Contain(c => c.Name == "新分类1");
        categories.Should().Contain(c => c.Name == "新分类2");

        var transactions = await _context.Transactions
            .Where(t => t.UserId == _userId)
            .Include(t => t.Category)
            .ToListAsync();
        transactions.Count.Should().Be(4);
        transactions.Should().Contain(t => t.Category!.Name == "新分类1");
        transactions.Should().Contain(t => t.Category!.Name == "新分类2");
    }

    [Fact]
    public async Task ImportTransactions_WithVariousAmountFormats_ShouldParseCorrectly()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        await CreateTestCategoryAsync("餐饮", CategoryType.Expense);

        var csvContent = @"日期,分类,金额,备注
2026-01-15,餐饮,35.50,普通
2026-01-16,餐饮,¥100,人民币符号
2026-01-17,餐饮,$50,美元符号
2026-01-18,餐饮,""1,234.50"",千分位
2026-01-19,餐饮,""¥2,000.99"",混合格式";

        using var stream = CreateCsvStream(csvContent);

        // Act
        var result = await _service.ImportTransactionsAsync(stream, _userId, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.ErrorCount.Should().Be(0);
        result.AddedCount.Should().Be(5);
        result.IsSuccess.Should().BeTrue();

        var transactions = await _context.Transactions
            .Where(t => t.UserId == _userId)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync();
        transactions.Count.Should().Be(5);
        transactions[0].Amount.Should().Be(35.50m);
        transactions[1].Amount.Should().Be(100m);
        transactions[2].Amount.Should().Be(50m);
        transactions[3].Amount.Should().Be(1234.50m);
        transactions[4].Amount.Should().Be(2000.99m);
    }

    [Fact]
    public async Task ImportTransactions_WithInvalidAccount_ShouldThrowException()
    {
        // Arrange
        var invalidAccountId = Guid.NewGuid();
        var csvContent = @"日期,分类,金额,备注
2026-01-15,餐饮,35.50,午餐";

        using var stream = CreateCsvStream(csvContent);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ImportTransactionsAsync(stream, _userId, invalidAccountId));

        exception.Message.Should().Contain("Account not found");
    }

    [Fact]
    public async Task ImportTransactions_WithEmptyCsv_ShouldReturnEmptyResult()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var csvContent = "日期,分类,金额,备注\n";

        using var stream = CreateCsvStream(csvContent);

        // Act
        var result = await _service.ImportTransactionsAsync(stream, _userId, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(0);
        result.AddedCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
