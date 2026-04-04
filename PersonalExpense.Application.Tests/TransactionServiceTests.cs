using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PersonalExpense.Application.Tests;

public class TransactionServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly TransactionService _transactionService;
    private readonly Guid _userId;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new ApplicationDbContext(options);
        _transactionService = new TransactionService(_context);
        _userId = Guid.NewGuid();

        // 初始化测试数据
        SeedTestData();
    }

    private void SeedTestData()
    {
        // 创建测试账户
        var account1 = new Account
        {
            Id = Guid.NewGuid(),
            Name = "Test Account 1",
            Type = AccountType.BankCard,
            Balance = 1000m,
            UserId = _userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var account2 = new Account
        {
            Id = Guid.NewGuid(),
            Name = "Test Account 2",
            Type = AccountType.Cash,
            Balance = 500m,
            UserId = _userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.AddRange(account1, account2);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateTransaction_Income_ShouldIncreaseAccountBalance()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync(a => a.UserId == _userId);
        var initialBalance = account.Balance;
        var amount = 500m;

        var transaction = new Transaction
        {
            Type = TransactionType.Income,
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Description = "Test Income",
            AccountId = account.Id
        };

        // Act
        await _transactionService.CreateTransactionAsync(transaction, _userId);

        // Assert
        var updatedAccount = await _context.Accounts.FirstAsync(a => a.Id == account.Id);
        Assert.Equal(initialBalance + amount, updatedAccount.Balance);
    }

    [Fact]
    public async Task CreateTransaction_Expense_ShouldDecreaseAccountBalance()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync(a => a.UserId == _userId);
        var initialBalance = account.Balance;
        var amount = 200m;

        var transaction = new Transaction
        {
            Type = TransactionType.Expense,
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Description = "Test Expense",
            AccountId = account.Id
        };

        // Act
        await _transactionService.CreateTransactionAsync(transaction, _userId);

        // Assert
        var updatedAccount = await _context.Accounts.FirstAsync(a => a.Id == account.Id);
        Assert.Equal(initialBalance - amount, updatedAccount.Balance);
    }

    [Fact]
    public async Task CreateTransaction_Transfer_ShouldTransferAmountBetweenAccounts()
    {
        // Arrange
        var accounts = await _context.Accounts.Where(a => a.UserId == _userId).ToListAsync();
        var fromAccount = accounts[0];
        var toAccount = accounts[1];
        var initialFromBalance = fromAccount.Balance;
        var initialToBalance = toAccount.Balance;
        var amount = 300m;

        var transaction = new Transaction
        {
            Type = TransactionType.Transfer,
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Description = "Test Transfer",
            AccountId = fromAccount.Id,
            TransferToAccountId = toAccount.Id
        };

        // Act
        await _transactionService.CreateTransactionAsync(transaction, _userId);

        // Assert
        var updatedFromAccount = await _context.Accounts.FirstAsync(a => a.Id == fromAccount.Id);
        var updatedToAccount = await _context.Accounts.FirstAsync(a => a.Id == toAccount.Id);
        Assert.Equal(initialFromBalance - amount, updatedFromAccount.Balance);
        Assert.Equal(initialToBalance + amount, updatedToAccount.Balance);
    }

    [Fact]
    public async Task UpdateTransaction_ShouldAdjustBalancesCorrectly()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync(a => a.UserId == _userId);
        var initialBalance = account.Balance;
        var originalAmount = 200m;
        var newAmount = 300m;

        // 创建初始交易
        var transaction = new Transaction
        {
            Type = TransactionType.Expense,
            Amount = originalAmount,
            TransactionDate = DateTime.UtcNow,
            Description = "Original Expense",
            AccountId = account.Id
        };

        var createdTransaction = await _transactionService.CreateTransactionAsync(transaction, _userId);

        // Act - 更新交易金额
        var updatedTransaction = new Transaction
        {
            Type = TransactionType.Expense,
            Amount = newAmount,
            TransactionDate = DateTime.UtcNow,
            Description = "Updated Expense",
            AccountId = account.Id
        };

        await _transactionService.UpdateTransactionAsync(createdTransaction.Id, updatedTransaction, _userId);

        // Assert
        var updatedAccount = await _context.Accounts.FirstAsync(a => a.Id == account.Id);
        Assert.Equal(initialBalance - newAmount, updatedAccount.Balance);
    }

    [Fact]
    public async Task DeleteTransaction_ShouldRollbackBalanceChanges()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync(a => a.UserId == _userId);
        var initialBalance = account.Balance;
        var amount = 200m;

        // 创建交易
        var transaction = new Transaction
        {
            Type = TransactionType.Expense,
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Description = "Test Expense",
            AccountId = account.Id
        };

        var createdTransaction = await _transactionService.CreateTransactionAsync(transaction, _userId);

        // Act - 删除交易
        await _transactionService.DeleteTransactionAsync(createdTransaction.Id, _userId);

        // Assert
        var updatedAccount = await _context.Accounts.FirstAsync(a => a.Id == account.Id);
        Assert.Equal(initialBalance, updatedAccount.Balance);
    }

    [Fact]
    public async Task CreateTransaction_InvalidAccount_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var transaction = new Transaction
        {
            Type = TransactionType.Income,
            Amount = 100m,
            TransactionDate = DateTime.UtcNow,
            Description = "Test Income",
            AccountId = Guid.NewGuid() // 不存在的账户ID
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionService.CreateTransactionAsync(transaction, _userId)
        );
    }

    [Fact]
    public async Task CreateTransaction_TransferToInvalidAccount_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync(a => a.UserId == _userId);

        var transaction = new Transaction
        {
            Type = TransactionType.Transfer,
            Amount = 100m,
            TransactionDate = DateTime.UtcNow,
            Description = "Test Transfer",
            AccountId = account.Id,
            TransferToAccountId = Guid.NewGuid() // 不存在的账户ID
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionService.CreateTransactionAsync(transaction, _userId)
        );
    }
}
