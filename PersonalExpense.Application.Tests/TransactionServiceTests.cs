using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Tests;

public class TransactionServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly TransactionService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        
        _context = new ApplicationDbContext(options);
        _service = new TransactionService(_context);

        // 初始化测试数据
        InitializeTestData();
    }

    private void InitializeTestData()
    {
        // 清空数据库
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        // 添加测试账户
        var cashAccount = new Account
        {
            Id = Guid.NewGuid(),
            Name = "现金",
            Balance = 1000,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        var bankAccount = new Account
        {
            Id = Guid.NewGuid(),
            Name = "银行",
            Balance = 5000,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        // 添加测试分类
        var foodCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "餐饮",
            Type = CategoryType.Expense,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        var salaryCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "工资",
            Type = CategoryType.Income,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.AddRange(cashAccount, bankAccount);
        _context.Categories.AddRange(foodCategory, salaryCategory);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateTransactionAsync_IncomeTransaction_IncreasesAccountBalance()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync(a => a.Name == "现金" && a.UserId == _userId);
        var initialBalance = account.Balance;

        var transactionDto = new TransactionCreateDto
        {
            Type = TransactionType.Income,
            Amount = 500,
            TransactionDate = DateTime.UtcNow,
            Description = "测试收入",
            AccountId = account.Id,
            CategoryId = (await _context.Categories.FirstAsync(c => c.Name == "工资" && c.UserId == _userId)).Id
        };

        // Act
        var result = await _service.CreateTransactionAsync(transactionDto, _userId);

        // Assert
        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        Assert.Equal(initialBalance + 500, updatedAccount.Balance);
        Assert.Equal(TransactionType.Income, result.Type);
        Assert.Equal(500, result.Amount);
    }

    [Fact]
    public async Task CreateTransactionAsync_ExpenseTransaction_DecreasesAccountBalance()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync(a => a.Name == "现金" && a.UserId == _userId);
        var initialBalance = account.Balance;

        var transactionDto = new TransactionCreateDto
        {
            Type = TransactionType.Expense,
            Amount = 200,
            TransactionDate = DateTime.UtcNow,
            Description = "测试支出",
            AccountId = account.Id,
            CategoryId = (await _context.Categories.FirstAsync(c => c.Name == "餐饮" && c.UserId == _userId)).Id
        };

        // Act
        var result = await _service.CreateTransactionAsync(transactionDto, _userId);

        // Assert
        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        Assert.Equal(initialBalance - 200, updatedAccount.Balance);
        Assert.Equal(TransactionType.Expense, result.Type);
        Assert.Equal(200, result.Amount);
    }

    [Fact]
    public async Task CreateTransactionAsync_TransferTransaction_UpdatesBothAccountBalances()
    {
        // Arrange
        var fromAccount = await _context.Accounts.FirstAsync(a => a.Name == "现金" && a.UserId == _userId);
        var toAccount = await _context.Accounts.FirstAsync(a => a.Name == "银行" && a.UserId == _userId);
        var initialFromBalance = fromAccount.Balance;
        var initialToBalance = toAccount.Balance;

        var transactionDto = new TransactionCreateDto
        {
            Type = TransactionType.Transfer,
            Amount = 300,
            TransactionDate = DateTime.UtcNow,
            Description = "测试转账",
            AccountId = fromAccount.Id,
            TransferToAccountId = toAccount.Id
        };

        // Act
        var result = await _service.CreateTransactionAsync(transactionDto, _userId);

        // Assert
        var updatedFromAccount = await _context.Accounts.FindAsync(fromAccount.Id);
        var updatedToAccount = await _context.Accounts.FindAsync(toAccount.Id);
        
        Assert.Equal(initialFromBalance - 300, updatedFromAccount.Balance);
        Assert.Equal(initialToBalance + 300, updatedToAccount.Balance);
        Assert.Equal(TransactionType.Transfer, result.Type);
        Assert.Equal(300, result.Amount);
    }

    [Fact]
    public async Task CreateTransactionAsync_InvalidAccount_ThrowsExceptionAndRollsBack()
    {
        // Arrange
        var invalidAccountId = Guid.NewGuid(); // 不存在的账户ID
        var initialBalance = (await _context.Accounts.FirstAsync(a => a.Name == "现金" && a.UserId == _userId)).Balance;

        var transactionDto = new TransactionCreateDto
        {
            Type = TransactionType.Income,
            Amount = 500,
            TransactionDate = DateTime.UtcNow,
            Description = "测试收入",
            AccountId = invalidAccountId,
            CategoryId = (await _context.Categories.FirstAsync(c => c.Name == "工资" && a.UserId == _userId)).Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreateTransactionAsync(transactionDto, _userId));

        // 验证余额没有变化（回滚成功）
        var account = await _context.Accounts.FirstAsync(a => a.Name == "现金" && a.UserId == _userId);
        Assert.Equal(initialBalance, account.Balance);
    }

    [Fact]
    public async Task UpdateTransactionAsync_ChangesTransactionType_UpdatesBalancesCorrectly()
    {
        // Arrange
        var cashAccount = await _context.Accounts.FirstAsync(a => a.Name == "现金" && a.UserId == _userId);
        var bankAccount = await _context.Accounts.FirstAsync(a => a.Name == "银行" && a.UserId == _userId);
        var initialCashBalance = cashAccount.Balance;
        var initialBankBalance = bankAccount.Balance;

        // 先创建一个收入交易
        var createDto = new TransactionCreateDto
        {
            Type = TransactionType.Income,
            Amount = 500,
            TransactionDate = DateTime.UtcNow,
            Description = "原始收入",
            AccountId = cashAccount.Id,
            CategoryId = (await _context.Categories.FirstAsync(c => c.Name == "工资" && c.UserId == _userId)).Id
        };

        var createdTransaction = await _service.CreateTransactionAsync(createDto, _userId);

        // 更新为支出交易
        var updateDto = new TransactionUpdateDto
        {
            Type = TransactionType.Expense,
            Amount = 300,
            TransactionDate = DateTime.UtcNow,
            Description = "更新为支出",
            AccountId = bankAccount.Id, // 变更账户
            CategoryId = (await _context.Categories.FirstAsync(c => c.Name == "餐饮" && c.UserId == _userId)).Id
        };

        // Act
        var updatedTransaction = await _service.UpdateTransactionAsync(createdTransaction.Id, updateDto, _userId);

        // Assert
        var updatedCashAccount = await _context.Accounts.FindAsync(cashAccount.Id);
        var updatedBankAccount = await _context.Accounts.FindAsync(bankAccount.Id);

        // 现金账户应恢复到初始余额（因为我们从收入变成了支出，并且变更了账户）
        Assert.Equal(initialCashBalance, updatedCashAccount.Balance);
        // 银行账户应该减少300
        Assert.Equal(initialBankBalance - 300, updatedBankAccount.Balance);
        Assert.Equal(TransactionType.Expense, updatedTransaction.Type);
        Assert.Equal(300, updatedTransaction.Amount);
    }

    [Fact]
    public async Task DeleteTransactionAsync_RemovesTransactionAndRevertsBalance()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync(a => a.Name == "现金" && a.UserId == _userId);
        var initialBalance = account.Balance;

        // 创建一个支出交易
        var createDto = new TransactionCreateDto
        {
            Type = TransactionType.Expense,
            Amount = 200,
            TransactionDate = DateTime.UtcNow,
            Description = "要删除的支出",
            AccountId = account.Id,
            CategoryId = (await _context.Categories.FirstAsync(c => c.Name == "餐饮" && c.UserId == _userId)).Id
        };

        var createdTransaction = await _service.CreateTransactionAsync(createDto, _userId);
        var afterCreateBalance = (await _context.Accounts.FindAsync(account.Id)).Balance;
        Assert.Equal(initialBalance - 200, afterCreateBalance);

        // Act
        await _service.DeleteTransactionAsync(createdTransaction.Id, _userId);

        // Assert
        var afterDeleteAccount = await _context.Accounts.FindAsync(account.Id);
        Assert.Equal(initialBalance, afterDeleteAccount.Balance);
        
        var deletedTransaction = await _context.Transactions.FindAsync(createdTransaction.Id);
        Assert.Null(deletedTransaction);
    }

    [Fact]
    public async Task DeleteTransactionAsync_TransferTransaction_RevertsBothBalances()
    {
        // Arrange
        var fromAccount = await _context.Accounts.FirstAsync(a => a.Name == "现金" && a.UserId == _userId);
        var toAccount = await _context.Accounts.FirstAsync(a => a.Name == "银行" && a.UserId == _userId);
        var initialFromBalance = fromAccount.Balance;
        var initialToBalance = toAccount.Balance;

        // 创建一个转账交易
        var createDto = new TransactionCreateDto
        {
            Type = TransactionType.Transfer,
            Amount = 300,
            TransactionDate = DateTime.UtcNow,
            Description = "要删除的转账",
            AccountId = fromAccount.Id,
            TransferToAccountId = toAccount.Id
        };

        var createdTransaction = await _service.CreateTransactionAsync(createDto, _userId);

        // Act
        await _service.DeleteTransactionAsync(createdTransaction.Id, _userId);

        // Assert
        var updatedFromAccount = await _context.Accounts.FindAsync(fromAccount.Id);
        var updatedToAccount = await _context.Accounts.FindAsync(toAccount.Id);

        Assert.Equal(initialFromBalance, updatedFromAccount.Balance);
        Assert.Equal(initialToBalance, updatedToAccount.Balance);
    }
}