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

        if (initialBalance != 0)
        {
            var initialBalanceTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Type = initialBalance > 0 ? TransactionType.Income : TransactionType.Expense,
                Amount = Math.Abs(initialBalance),
                TransactionDate = DateTime.UtcNow,
                Description = "初始余额",
                CreatedAt = DateTime.UtcNow,
                UserId = _userId,
                AccountId = account.Id
            };

            _context.Transactions.Add(initialBalanceTransaction);
            await _context.SaveChangesAsync();
        }

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
        var nonInitialBalanceTransactions = transactions.Where(t => t.Description != "初始余额").ToList();
        nonInitialBalanceTransactions.Should().BeEmpty();

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

    #region Pair Transfer Tests (成对流水转账)

    [Fact]
    public async Task CreateTransfer_ShouldCreateTwoLinkedTransactions()
    {
        // Arrange
        var fromAccount = await CreateTestAccountAsync("Cash", 5000);
        var toAccount = await CreateTestAccountAsync("Bank Card", 10000);
        var fromInitial = fromAccount.Balance;
        var toInitial = toAccount.Balance;
        var transferAmount = 1000;

        var dto = new TransferCreateDto(
            Amount: transferAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Monthly transfer",
            AttachmentUrl: null,
            FromAccountId: fromAccount.Id,
            ToAccountId: toAccount.Id
        );

        // Act
        var result = await _service.CreateTransferAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.OutgoingTransaction.Should().NotBeNull();
        result.IncomingTransaction.Should().NotBeNull();

        result.OutgoingTransaction.Type.Should().Be(TransactionType.Expense);
        result.OutgoingTransaction.Amount.Should().Be(transferAmount);
        result.OutgoingTransaction.AccountId.Should().Be(fromAccount.Id);

        result.IncomingTransaction.Type.Should().Be(TransactionType.Income);
        result.IncomingTransaction.Amount.Should().Be(transferAmount);
        result.IncomingTransaction.AccountId.Should().Be(toAccount.Id);

        result.OutgoingTransaction.RelatedTransactionId.Should().Be(result.IncomingTransaction.Id);
        result.IncomingTransaction.RelatedTransactionId.Should().Be(result.OutgoingTransaction.Id);

        var updatedFromAccount = await _context.Accounts.FindAsync(fromAccount.Id);
        var updatedToAccount = await _context.Accounts.FindAsync(toAccount.Id);

        updatedFromAccount.Should().NotBeNull();
        updatedToAccount.Should().NotBeNull();
        updatedFromAccount!.Balance.Should().Be(fromInitial - transferAmount);
        updatedToAccount!.Balance.Should().Be(toInitial + transferAmount);

        var transactions = await _context.Transactions.ToListAsync();
        var transferTransactions = transactions.Where(t => t.RelatedTransactionId.HasValue).ToList();
        transferTransactions.Count.Should().Be(2);
    }

    [Fact]
    public async Task CreateTransfer_WithZeroAmount_ShouldThrowException()
    {
        // Arrange
        var fromAccount = await CreateTestAccountAsync("Cash", 5000);
        var toAccount = await CreateTestAccountAsync("Bank Card", 10000);

        var dto = new TransferCreateDto(
            Amount: 0,
            TransactionDate: DateTime.UtcNow,
            Description: "Test",
            AttachmentUrl: null,
            FromAccountId: fromAccount.Id,
            ToAccountId: toAccount.Id
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateTransferAsync(dto, _userId));

        exception.Message.Should().Contain("greater than 0");
    }

    [Fact]
    public async Task CreateTransfer_ToSameAccount_ShouldThrowException()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 5000);

        var dto = new TransferCreateDto(
            Amount: 1000,
            TransactionDate: DateTime.UtcNow,
            Description: "Test",
            AttachmentUrl: null,
            FromAccountId: account.Id,
            ToAccountId: account.Id
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateTransferAsync(dto, _userId));

        exception.Message.Should().Contain("same account");
    }

    [Fact]
    public async Task CreateTransfer_WithInsufficientBalance_ShouldThrowException()
    {
        // Arrange
        var fromAccount = await CreateTestAccountAsync("Cash", 500);
        var toAccount = await CreateTestAccountAsync("Bank Card", 10000);

        var dto = new TransferCreateDto(
            Amount: 1000,
            TransactionDate: DateTime.UtcNow,
            Description: "Test",
            AttachmentUrl: null,
            FromAccountId: fromAccount.Id,
            ToAccountId: toAccount.Id
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateTransferAsync(dto, _userId));

        exception.Message.Should().Contain("Insufficient balance");
    }

    [Fact]
    public async Task CreateTransfer_WithInvalidFromAccount_ShouldNotCreateAnyTransactions()
    {
        // Arrange
        var invalidFromAccountId = Guid.NewGuid();
        var toAccount = await CreateTestAccountAsync("Bank Card", 10000);
        var toInitial = toAccount.Balance;

        var dto = new TransferCreateDto(
            Amount: 1000,
            TransactionDate: DateTime.UtcNow,
            Description: "Test",
            AttachmentUrl: null,
            FromAccountId: invalidFromAccountId,
            ToAccountId: toAccount.Id
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateTransferAsync(dto, _userId));

        exception.Message.Should().Contain("From account not found");

        var transactions = await _context.Transactions.ToListAsync();
        var nonInitialBalanceTransactions = transactions.Where(t => t.Description != "初始余额").ToList();
        nonInitialBalanceTransactions.Should().BeEmpty();

        var updatedToAccount = await _context.Accounts.FindAsync(toAccount.Id);
        updatedToAccount.Should().NotBeNull();
        updatedToAccount!.Balance.Should().Be(toInitial);
    }

    [Fact]
    public async Task DeleteTransferTransaction_ShouldDeleteBothLinkedTransactions()
    {
        // Arrange
        var fromAccount = await CreateTestAccountAsync("Cash", 5000);
        var toAccount = await CreateTestAccountAsync("Bank Card", 10000);
        var fromInitial = fromAccount.Balance;
        var toInitial = toAccount.Balance;
        var transferAmount = 1000;

        var dto = new TransferCreateDto(
            Amount: transferAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Test transfer",
            AttachmentUrl: null,
            FromAccountId: fromAccount.Id,
            ToAccountId: toAccount.Id
        );

        var result = await _service.CreateTransferAsync(dto, _userId);

        // Act
        await _service.DeleteTransactionAsync(result.OutgoingTransaction.Id, _userId);

        // Assert
        var transactions = await _context.Transactions.ToListAsync();
        var transferTransactions = transactions.Where(t => t.RelatedTransactionId.HasValue).ToList();
        transferTransactions.Should().BeEmpty();

        var updatedFromAccount = await _context.Accounts.FindAsync(fromAccount.Id);
        var updatedToAccount = await _context.Accounts.FindAsync(toAccount.Id);

        updatedFromAccount.Should().NotBeNull();
        updatedToAccount.Should().NotBeNull();
        updatedFromAccount!.Balance.Should().Be(fromInitial);
        updatedToAccount!.Balance.Should().Be(toInitial);
    }

    [Fact]
    public async Task UpdateTransferTransaction_ShouldThrowException()
    {
        // Arrange
        var fromAccount = await CreateTestAccountAsync("Cash", 5000);
        var toAccount = await CreateTestAccountAsync("Bank Card", 10000);

        var dto = new TransferCreateDto(
            Amount: 1000,
            TransactionDate: DateTime.UtcNow,
            Description: "Test transfer",
            AttachmentUrl: null,
            FromAccountId: fromAccount.Id,
            ToAccountId: toAccount.Id
        );

        var result = await _service.CreateTransferAsync(dto, _userId);

        var updateDto = new TransactionUpdateDto(
            Type: TransactionType.Expense,
            Amount: 2000,
            TransactionDate: DateTime.UtcNow,
            Description: "Updated",
            AttachmentUrl: null,
            AccountId: fromAccount.Id,
            CategoryId: null,
            TransferToAccountId: toAccount.Id
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateTransactionAsync(result.OutgoingTransaction.Id, updateDto, _userId));

        exception.Message.Should().Contain("Delete and recreate");
    }

    #endregion

    #region Balance History Tests (余额变动查询)

    [Fact]
    public async Task GetAccountBalanceHistory_ShouldReturnCorrectHistory()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var initialBalance = account.Balance;

        var income1 = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: 500,
            TransactionDate: DateTime.UtcNow.AddDays(-2),
            Description: "Salary",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );
        await _service.CreateTransactionAsync(income1, _userId);

        var expense1 = new TransactionCreateDto(
            Type: TransactionType.Expense,
            Amount: 200,
            TransactionDate: DateTime.UtcNow.AddDays(-1),
            Description: "Groceries",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );
        await _service.CreateTransactionAsync(expense1, _userId);

        // Act
        var result = await _service.GetAccountBalanceHistoryAsync(account.Id, _userId, null, null);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(account.Id);
        result.TotalIncome.Should().Be(500);
        result.TotalExpense.Should().Be(200);
        result.NetChange.Should().Be(300);
        result.StartingBalance.Should().Be(initialBalance);
        result.EndingBalance.Should().Be(initialBalance + 300);

        result.BalanceHistory.Should().HaveCount(2);
        result.BalanceHistory[0].BalanceAfterTransaction.Should().Be(initialBalance + 500);
        result.BalanceHistory[1].BalanceAfterTransaction.Should().Be(initialBalance + 500 - 200);
    }

    [Fact]
    public async Task GetAccountBalanceHistory_WithTransfer_ShouldShowCorrectBalance()
    {
        // Arrange
        var cashAccount = await CreateTestAccountAsync("Cash", 5000);
        var bankAccount = await CreateTestAccountAsync("Bank Card", 10000);
        var cashInitial = cashAccount.Balance;
        var bankInitial = bankAccount.Balance;

        var transferDto = new TransferCreateDto(
            Amount: 1000,
            TransactionDate: DateTime.UtcNow,
            Description: "Transfer",
            AttachmentUrl: null,
            FromAccountId: cashAccount.Id,
            ToAccountId: bankAccount.Id
        );

        await _service.CreateTransferAsync(transferDto, _userId);

        // Act
        var cashHistory = await _service.GetAccountBalanceHistoryAsync(cashAccount.Id, _userId, null, null);
        var bankHistory = await _service.GetAccountBalanceHistoryAsync(bankAccount.Id, _userId, null, null);

        // Assert
        cashHistory.Should().NotBeNull();
        cashHistory.NetChange.Should().Be(-1000);
        cashHistory.EndingBalance.Should().Be(cashInitial - 1000);
        cashHistory.BalanceHistory.Should().HaveCount(1);
        cashHistory.BalanceHistory[0].RelatedTransactionId.Should().NotBeNull();

        bankHistory.Should().NotBeNull();
        bankHistory.NetChange.Should().Be(1000);
        bankHistory.EndingBalance.Should().Be(bankInitial + 1000);
        bankHistory.BalanceHistory.Should().HaveCount(1);
        bankHistory.BalanceHistory[0].RelatedTransactionId.Should().NotBeNull();
    }

    #endregion

    #region Reconciliation Tests (对账功能)

    [Fact]
    public async Task ReconcileAccount_WithBalancedAccount_ShouldReturnIsBalancedTrue()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var income1 = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: 500,
            TransactionDate: DateTime.UtcNow,
            Description: "Salary",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );
        await _service.CreateTransactionAsync(income1, _userId);

        var reconciliationService = new ReconciliationService(_context);

        // Act
        var result = await reconciliationService.ReconcileAccountAsync(account.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result.IsBalanced.Should().BeTrue();
        result.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconcileAccount_WithTransferPair_ShouldReturnIsBalancedTrue()
    {
        // Arrange
        var cashAccount = await CreateTestAccountAsync("Cash", 5000);
        var bankAccount = await CreateTestAccountAsync("Bank Card", 10000);

        var transferDto = new TransferCreateDto(
            Amount: 1000,
            TransactionDate: DateTime.UtcNow,
            Description: "Transfer",
            AttachmentUrl: null,
            FromAccountId: cashAccount.Id,
            ToAccountId: bankAccount.Id
        );

        await _service.CreateTransferAsync(transferDto, _userId);

        var reconciliationService = new ReconciliationService(_context);

        // Act
        var cashResult = await reconciliationService.ReconcileAccountAsync(cashAccount.Id, _userId);
        var bankResult = await reconciliationService.ReconcileAccountAsync(bankAccount.Id, _userId);

        // Assert
        cashResult.Should().NotBeNull();
        bankResult.Should().NotBeNull();
        cashResult.IsBalanced.Should().BeTrue();
        bankResult.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task DetectTransferDiscrepancies_WithValidTransfers_ShouldReturnEmpty()
    {
        // Arrange
        var cashAccount = await CreateTestAccountAsync("Cash", 5000);
        var bankAccount = await CreateTestAccountAsync("Bank Card", 10000);

        var transferDto = new TransferCreateDto(
            Amount: 1000,
            TransactionDate: DateTime.UtcNow,
            Description: "Transfer",
            AttachmentUrl: null,
            FromAccountId: cashAccount.Id,
            ToAccountId: bankAccount.Id
        );

        await _service.CreateTransferAsync(transferDto, _userId);

        var reconciliationService = new ReconciliationService(_context);

        // Act
        var result = await reconciliationService.DetectTransferDiscrepanciesAsync(_userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconcileAllAccounts_ShouldReturnAllAccountsResults()
    {
        // Arrange
        var cashAccount = await CreateTestAccountAsync("Cash", 5000);
        var bankAccount = await CreateTestAccountAsync("Bank Card", 10000);

        var reconciliationService = new ReconciliationService(_context);

        // Act
        var results = await reconciliationService.ReconcileAllAccountsAsync(_userId);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);
        results.All(r => r.IsBalanced).Should().BeTrue();
    }

    #endregion

    #region Reconciliation Core Logic Tests (对账核心逻辑测试)

    [Fact]
    public async Task ReconcileAccount_AfterTamperingBalance_ShouldDetectUnbalanced()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var originalBalance = account.Balance;

        var incomeDto = new TransactionCreateDto(
            Type: TransactionType.Income,
            Amount: 500,
            TransactionDate: DateTime.UtcNow,
            Description: "Salary",
            AttachmentUrl: null,
            AccountId: account.Id,
            CategoryId: null,
            TransferToAccountId: null
        );
        await _service.CreateTransactionAsync(incomeDto, _userId);

        var accountAfterTransaction = await _context.Accounts.FindAsync(account.Id);
        accountAfterTransaction.Should().NotBeNull();
        var expectedBalanceAfterTransaction = accountAfterTransaction!.Balance;

        // 手动篡改账户余额（模拟有人直接修改数据库）
        accountAfterTransaction.Balance += 100;
        await _context.SaveChangesAsync();

        var reconciliationService = new ReconciliationService(_context);

        // Act
        var result = await reconciliationService.ReconcileAccountAsync(account.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result.IsBalanced.Should().BeFalse();
        result.ExpectedBalance.Should().Be(expectedBalanceAfterTransaction);
        result.ActualBalance.Should().Be(expectedBalanceAfterTransaction + 100);
        result.Discrepancy.Should().Be(100);
        result.Discrepancies.Should().NotBeEmpty();
        result.Discrepancies.Any(d => d.Type == "BalanceMismatch").Should().BeTrue();
    }

    [Fact]
    public async Task ReconcileAccount_AfterValidTransfer_ShouldStayBalancedWithZeroDiscrepancy()
    {
        // Arrange
        var cashAccount = await CreateTestAccountAsync("Cash", 5000);
        var bankAccount = await CreateTestAccountAsync("Bank Card", 10000);
        var cashInitial = cashAccount.Balance;
        var bankInitial = bankAccount.Balance;
        var transferAmount = 1000;

        var transferDto = new TransferCreateDto(
            Amount: transferAmount,
            TransactionDate: DateTime.UtcNow,
            Description: "Monthly transfer",
            AttachmentUrl: null,
            FromAccountId: cashAccount.Id,
            ToAccountId: bankAccount.Id
        );

        await _service.CreateTransferAsync(transferDto, _userId);

        var reconciliationService = new ReconciliationService(_context);

        // Act
        var cashResult = await reconciliationService.ReconcileAccountAsync(cashAccount.Id, _userId);
        var bankResult = await reconciliationService.ReconcileAccountAsync(bankAccount.Id, _userId);

        // Assert - Cash Account
        cashResult.Should().NotBeNull();
        cashResult.IsBalanced.Should().BeTrue();
        cashResult.ExpectedBalance.Should().Be(cashInitial - transferAmount);
        cashResult.ActualBalance.Should().Be(cashInitial - transferAmount);
        cashResult.Discrepancy.Should().Be(0);
        cashResult.Discrepancies.Should().BeEmpty();

        // Assert - Bank Account
        bankResult.Should().NotBeNull();
        bankResult.IsBalanced.Should().BeTrue();
        bankResult.ExpectedBalance.Should().Be(bankInitial + transferAmount);
        bankResult.ActualBalance.Should().Be(bankInitial + transferAmount);
        bankResult.Discrepancy.Should().Be(0);
        bankResult.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconcileAccount_WithTolerance_ShouldIgnoreSmallDifferences()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var accountInDb = await _context.Accounts.FindAsync(account.Id);
        accountInDb.Should().NotBeNull();
        
        // 篡改余额 0.005，小于容差 0.01，应该被忽略
        accountInDb!.Balance += 0.005m;
        await _context.SaveChangesAsync();

        var reconciliationService = new ReconciliationService(_context);

        // Act
        var result = await reconciliationService.ReconcileAccountAsync(account.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result.IsBalanced.Should().BeTrue();
        result.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconcileAccount_WithSignificantDifference_ShouldDetectUnbalanced()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var accountInDb = await _context.Accounts.FindAsync(account.Id);
        accountInDb.Should().NotBeNull();
        
        // 篡改余额 0.02，大于容差 0.01，应该被检测到
        accountInDb!.Balance += 0.02m;
        await _context.SaveChangesAsync();

        var reconciliationService = new ReconciliationService(_context);

        // Act
        var result = await reconciliationService.ReconcileAccountAsync(account.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result.IsBalanced.Should().BeFalse();
        result.Discrepancies.Should().NotBeEmpty();
        result.Discrepancies.Any(d => d.Type == "BalanceMismatch").Should().BeTrue();
    }

    #endregion
}
