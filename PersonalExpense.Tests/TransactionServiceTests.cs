using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

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
        var budgetService = new BudgetService(_context);
        _service = new TransactionService(_context, budgetService);
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
        result.Transaction.Should().NotBeNull();
        result.Transaction.Type.Should().Be(TransactionType.Income);
        result.Transaction.Amount.Should().Be(incomeAmount);

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

        var createdResult = await _service.CreateTransactionAsync(createDto, _userId);

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
        var result = await _service.UpdateTransactionAsync(createdResult.Transaction.Id, updateDto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Transaction.Amount.Should().Be(newIncome);

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

        var createdResult = await _service.CreateTransactionAsync(createDto, _userId);

        // Act
        await _service.DeleteTransactionAsync(createdResult.Transaction.Id, _userId);

        // Assert
        var deletedTransaction = await _context.Transactions.FindAsync(createdResult.Transaction.Id);
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
        result.Transaction.Type.Should().Be(TransactionType.Expense);
        result.Transaction.Amount.Should().Be(expenseAmount);

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

        var createdResult = await _service.CreateTransactionAsync(createDto, _userId);

        // Act
        await _service.DeleteTransactionAsync(createdResult.Transaction.Id, _userId);

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
        result.Transaction.Type.Should().Be(TransactionType.Transfer);
        result.Transaction.Amount.Should().Be(transferAmount);

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

        var createdResult = await _service.CreateTransactionAsync(createDto, _userId);

        // Act
        await _service.DeleteTransactionAsync(createdResult.Transaction.Id, _userId);

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

        var createdResult = await _service.CreateTransactionAsync(createDto, _userId);

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
            () => _service.UpdateTransactionAsync(createdResult.Transaction.Id, updateDto, _userId));

        exception.Message.Should().Contain("Account not found");

        var transaction = await _context.Transactions.FindAsync(createdResult.Transaction.Id);
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
        result.Transaction.Amount.Should().Be(0);

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

        var createdResult = await _service.CreateTransactionAsync(createDto, _userId);

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
        var result = await _service.UpdateTransactionAsync(createdResult.Transaction.Id, updateDto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Transaction.Type.Should().Be(TransactionType.Expense);

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
}
