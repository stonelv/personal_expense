using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.Exceptions;

namespace PersonalExpense.Tests;

public class TransactionServiceTests
{
    private readonly Guid _testUserId;
    private readonly Guid _testAccountId;
    private readonly Guid _testCategoryId;
    private readonly Guid _testTransferToAccountId;

    public TransactionServiceTests()
    {
        _testUserId = Guid.NewGuid();
        _testAccountId = Guid.NewGuid();
        _testCategoryId = Guid.NewGuid();
        _testTransferToAccountId = Guid.NewGuid();
    }

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        var user = new User { Id = _testUserId, UserName = "testuser", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);

        var account = new Account { Id = _testAccountId, Name = "Cash", Balance = 1000, UserId = _testUserId };
        context.Accounts.Add(account);

        var transferToAccount = new Account { Id = _testTransferToAccountId, Name = "Bank", Balance = 2000, UserId = _testUserId };
        context.Accounts.Add(transferToAccount);

        var category = new Category { Id = _testCategoryId, Name = "Food", Type = CategoryType.Expense, UserId = _testUserId };
        context.Categories.Add(category);

        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task CreateTransactionAsync_IncomeTransaction_ShouldIncreaseAccountBalance()
    {
        using var context = CreateInMemoryContext();
        var service = new TransactionService(context);

        var initialBalance = await context.Accounts.Where(a => a.Id == _testAccountId).Select(a => a.Balance).FirstAsync();

        var dto = new TransactionCreateDto
        {
            Type = TransactionType.Income,
            Amount = 500,
            TransactionDate = DateTime.UtcNow,
            Description = "Salary",
            AccountId = _testAccountId,
            CategoryId = _testCategoryId
        };

        var result = await service.CreateTransactionAsync(_testUserId, dto);
        var updatedAccount = await context.Accounts.FindAsync(_testAccountId);

        Assert.NotNull(result);
        Assert.Equal(initialBalance + 500, updatedAccount?.Balance);
    }

    [Fact]
    public async Task CreateTransactionAsync_ExpenseTransaction_ShouldDecreaseAccountBalance()
    {
        using var context = CreateInMemoryContext();
        var service = new TransactionService(context);

        var initialBalance = await context.Accounts.Where(a => a.Id == _testAccountId).Select(a => a.Balance).FirstAsync();

        var dto = new TransactionCreateDto
        {
            Type = TransactionType.Expense,
            Amount = 200,
            TransactionDate = DateTime.UtcNow,
            Description = "Groceries",
            AccountId = _testAccountId,
            CategoryId = _testCategoryId
        };

        var result = await service.CreateTransactionAsync(_testUserId, dto);
        var updatedAccount = await context.Accounts.FindAsync(_testAccountId);

        Assert.NotNull(result);
        Assert.Equal(initialBalance - 200, updatedAccount?.Balance);
    }

    [Fact]
    public async Task CreateTransactionAsync_TransferTransaction_ShouldUpdateBothBalances()
    {
        using var context = CreateInMemoryContext();
        var service = new TransactionService(context);

        var initialFromBalance = await context.Accounts.Where(a => a.Id == _testAccountId).Select(a => a.Balance).FirstAsync();
        var initialToBalance = await context.Accounts.Where(a => a.Id == _testTransferToAccountId).Select(a => a.Balance).FirstAsync();

        var dto = new TransactionCreateDto
        {
            Type = TransactionType.Transfer,
            Amount = 300,
            TransactionDate = DateTime.UtcNow,
            Description = "Transfer to Bank",
            AccountId = _testAccountId,
            TransferToAccountId = _testTransferToAccountId
        };

        var result = await service.CreateTransactionAsync(_testUserId, dto);
        var updatedFromAccount = await context.Accounts.FindAsync(_testAccountId);
        var updatedToAccount = await context.Accounts.FindAsync(_testTransferToAccountId);

        Assert.NotNull(result);
        Assert.Equal(initialFromBalance - 300, updatedFromAccount?.Balance);
        Assert.Equal(initialToBalance + 300, updatedToAccount?.Balance);
    }

    [Fact]
    public async Task CreateTransactionAsync_InvalidAccount_ShouldRollbackAndThrow()
    {
        using var context = CreateInMemoryContext();
        var service = new TransactionService(context);

        var invalidAccountId = Guid.NewGuid();
        var dto = new TransactionCreateDto
        {
            Type = TransactionType.Expense,
            Amount = 200,
            TransactionDate = DateTime.UtcNow,
            Description = "Test",
            AccountId = invalidAccountId,
            CategoryId = _testCategoryId
        };

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateTransactionAsync(_testUserId, dto));
        var account = await context.Accounts.FindAsync(_testAccountId);
        Assert.Equal(1000, account?.Balance);
    }

    [Fact]
    public async Task UpdateTransactionAsync_ChangeFromExpenseToIncome_ShouldUpdateBalance()
    {
        using var context = CreateInMemoryContext();
        var service = new TransactionService(context);

        var createDto = new TransactionCreateDto
        {
            Type = TransactionType.Expense,
            Amount = 200,
            TransactionDate = DateTime.UtcNow,
            Description = "Groceries",
            AccountId = _testAccountId,
            CategoryId = _testCategoryId
        };

        var created = await service.CreateTransactionAsync(_testUserId, createDto);
        var balanceAfterCreate = await context.Accounts.Where(a => a.Id == _testAccountId).Select(a => a.Balance).FirstAsync();
        Assert.Equal(800, balanceAfterCreate);

        var updateDto = new TransactionUpdateDto
        {
            Type = TransactionType.Income,
            Amount = 200,
            TransactionDate = DateTime.UtcNow,
            Description = "Refund",
            AccountId = _testAccountId,
            CategoryId = _testCategoryId
        };

        await service.UpdateTransactionAsync(created.Id, _testUserId, updateDto);
        var updatedAccount = await context.Accounts.FindAsync(_testAccountId);
        Assert.Equal(1200, updatedAccount?.Balance);
    }

    [Fact]
    public async Task DeleteTransactionAsync_ExpenseTransaction_ShouldRestoreBalance()
    {
        using var context = CreateInMemoryContext();
        var service = new TransactionService(context);

        var createDto = new TransactionCreateDto
        {
            Type = TransactionType.Expense,
            Amount = 300,
            TransactionDate = DateTime.UtcNow,
            Description = "Test",
            AccountId = _testAccountId,
            CategoryId = _testCategoryId
        };

        var created = await service.CreateTransactionAsync(_testUserId, createDto);
        var balanceAfterCreate = await context.Accounts.Where(a => a.Id == _testAccountId).Select(a => a.Balance).FirstAsync();
        Assert.Equal(700, balanceAfterCreate);

        await service.DeleteTransactionAsync(created.Id, _testUserId);
        var restoredAccount = await context.Accounts.FindAsync(_testAccountId);
        Assert.Equal(1000, restoredAccount?.Balance);
    }

    [Fact]
    public async Task DeleteTransactionAsync_TransferTransaction_ShouldRestoreBothBalances()
    {
        using var context = CreateInMemoryContext();
        var service = new TransactionService(context);

        var createDto = new TransactionCreateDto
        {
            Type = TransactionType.Transfer,
            Amount = 500,
            TransactionDate = DateTime.UtcNow,
            Description = "Transfer",
            AccountId = _testAccountId,
            TransferToAccountId = _testTransferToAccountId
        };

        var created = await service.CreateTransactionAsync(_testUserId, createDto);
        var fromAccountAfter = await context.Accounts.FindAsync(_testAccountId);
        var toAccountAfter = await context.Accounts.FindAsync(_testTransferToAccountId);
        Assert.Equal(500, fromAccountAfter?.Balance);
        Assert.Equal(2500, toAccountAfter?.Balance);

        await service.DeleteTransactionAsync(created.Id, _testUserId);
        var fromAccountRestored = await context.Accounts.FindAsync(_testAccountId);
        var toAccountRestored = await context.Accounts.FindAsync(_testTransferToAccountId);
        Assert.Equal(1000, fromAccountRestored?.Balance);
        Assert.Equal(2000, toAccountRestored?.Balance);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        using var context = CreateInMemoryContext();
        var service = new TransactionService(context);

        for (int i = 0; i < 15; i++)
        {
            var dto = new TransactionCreateDto
            {
                Type = TransactionType.Expense,
                Amount = 100 + i,
                TransactionDate = DateTime.UtcNow.AddDays(-i),
                Description = $"Test {i}",
                AccountId = _testAccountId,
                CategoryId = _testCategoryId
            };
            await service.CreateTransactionAsync(_testUserId, dto);
        }

        var parameters = new TransactionQueryParameters
        {
            PageNumber = 2,
            PageSize = 5
        };

        var result = await service.GetTransactionsAsync(_testUserId, parameters);

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithFiltering_ShouldReturnFilteredResults()
    {
        using var context = CreateInMemoryContext();
        var service = new TransactionService(context);

        for (int i = 0; i < 5; i++)
        {
            var dto = new TransactionCreateDto
            {
                Type = TransactionType.Expense,
                Amount = 100,
                TransactionDate = new DateTime(2024, 1, 15),
                Description = $"Expense {i}",
                AccountId = _testAccountId,
                CategoryId = _testCategoryId
            };
            await service.CreateTransactionAsync(_testUserId, dto);
        }

        for (int i = 0; i < 3; i++)
        {
            var dto = new TransactionCreateDto
            {
                Type = TransactionType.Income,
                Amount = 200,
                TransactionDate = new DateTime(2024, 1, 20),
                Description = $"Income {i}",
                AccountId = _testAccountId,
                CategoryId = _testCategoryId
            };
            await service.CreateTransactionAsync(_testUserId, dto);
        }

        var parameters = new TransactionQueryParameters
        {
            Type = TransactionType.Income,
            Year = 2024,
            Month = 1,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await service.GetTransactionsAsync(_testUserId, parameters);

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(TransactionType.Income, item.Type));
    }
}
