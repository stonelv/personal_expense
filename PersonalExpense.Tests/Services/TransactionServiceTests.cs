using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Tests.Services;

public class TransactionServiceTests
{
    private DbContextOptions<ApplicationDbContext> CreateInMemoryDbContextOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CreateTransactionAsync_Income_IncreasesAccountBalance()
    {
        var options = CreateInMemoryDbContextOptions();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        using (var context = new ApplicationDbContext(options))
        {
            context.Accounts.Add(new Account
            {
                Id = accountId,
                Name = "Test Account",
                Type = AccountType.Cash,
                Balance = 1000,
                UserId = userId
            });
            await context.SaveChangesAsync();
        }

        using (var context = new ApplicationDbContext(options))
        {
            var service = new TransactionService(context);
            var dto = new CreateTransactionDto(
                TransactionType.Income,
                500,
                DateTime.UtcNow,
                "Test Income",
                null,
                accountId,
                null,
                null
            );

            var result = await service.CreateTransactionAsync(dto, userId);

            Assert.NotNull(result);
            Assert.Equal(500, result.Amount);
            Assert.Equal(TransactionType.Income, result.Type);

            var account = await context.Accounts.FindAsync(accountId);
            Assert.NotNull(account);
            Assert.Equal(1500, account.Balance);
        }
    }

    [Fact]
    public async Task CreateTransactionAsync_Expense_DecreasesAccountBalance()
    {
        var options = CreateInMemoryDbContextOptions();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        using (var context = new ApplicationDbContext(options))
        {
            context.Accounts.Add(new Account
            {
                Id = accountId,
                Name = "Test Account",
                Type = AccountType.Cash,
                Balance = 1000,
                UserId = userId
            });
            await context.SaveChangesAsync();
        }

        using (var context = new ApplicationDbContext(options))
        {
            var service = new TransactionService(context);
            var dto = new CreateTransactionDto(
                TransactionType.Expense,
                300,
                DateTime.UtcNow,
                "Test Expense",
                null,
                accountId,
                null,
                null
            );

            var result = await service.CreateTransactionAsync(dto, userId);

            Assert.NotNull(result);
            Assert.Equal(300, result.Amount);
            Assert.Equal(TransactionType.Expense, result.Type);

            var account = await context.Accounts.FindAsync(accountId);
            Assert.NotNull(account);
            Assert.Equal(700, account.Balance);
        }
    }

    [Fact]
    public async Task CreateTransactionAsync_Transfer_UpdatesBothAccounts()
    {
        var options = CreateInMemoryDbContextOptions();
        var userId = Guid.NewGuid();
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        using (var context = new ApplicationDbContext(options))
        {
            context.Accounts.AddRange(
                new Account
                {
                    Id = fromAccountId,
                    Name = "From Account",
                    Type = AccountType.Cash,
                    Balance = 1000,
                    UserId = userId
                },
                new Account
                {
                    Id = toAccountId,
                    Name = "To Account",
                    Type = AccountType.BankCard,
                    Balance = 500,
                    UserId = userId
                }
            );
            await context.SaveChangesAsync();
        }

        using (var context = new ApplicationDbContext(options))
        {
            var service = new TransactionService(context);
            var dto = new CreateTransactionDto(
                TransactionType.Transfer,
                200,
                DateTime.UtcNow,
                "Test Transfer",
                null,
                fromAccountId,
                null,
                toAccountId
            );

            var result = await service.CreateTransactionAsync(dto, userId);

            Assert.NotNull(result);
            Assert.Equal(200, result.Amount);
            Assert.Equal(TransactionType.Transfer, result.Type);

            var fromAccount = await context.Accounts.FindAsync(fromAccountId);
            var toAccount = await context.Accounts.FindAsync(toAccountId);

            Assert.NotNull(fromAccount);
            Assert.NotNull(toAccount);
            Assert.Equal(800, fromAccount.Balance);
            Assert.Equal(700, toAccount.Balance);
        }
    }

    [Fact]
    public async Task CreateTransactionAsync_Rollback_OnError()
    {
        var options = CreateInMemoryDbContextOptions();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        using (var context = new ApplicationDbContext(options))
        {
            context.Accounts.Add(new Account
            {
                Id = accountId,
                Name = "Test Account",
                Type = AccountType.Cash,
                Balance = 1000,
                UserId = userId
            });
            await context.SaveChangesAsync();
        }

        using (var context = new ApplicationDbContext(options))
        {
            var service = new TransactionService(context);
            var dto = new CreateTransactionDto(
                TransactionType.Transfer,
                200,
                DateTime.UtcNow,
                "Test Transfer",
                null,
                accountId,
                null,
                Guid.NewGuid()
            );

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTransactionAsync(dto, userId));

            var account = await context.Accounts.FindAsync(accountId);
            Assert.NotNull(account);
            Assert.Equal(1000, account.Balance);

            var transactions = await context.Transactions.ToListAsync();
            Assert.Empty(transactions);
        }
    }

    [Fact]
    public async Task DeleteTransactionAsync_Income_RollsBackBalance()
    {
        var options = CreateInMemoryDbContextOptions();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        using (var context = new ApplicationDbContext(options))
        {
            context.Accounts.Add(new Account
            {
                Id = accountId,
                Name = "Test Account",
                Type = AccountType.Cash,
                Balance = 1500,
                UserId = userId
            });
            context.Transactions.Add(new Transaction
            {
                Id = transactionId,
                Type = TransactionType.Income,
                Amount = 500,
                TransactionDate = DateTime.UtcNow,
                AccountId = accountId,
                UserId = userId
            });
            await context.SaveChangesAsync();
        }

        using (var context = new ApplicationDbContext(options))
        {
            var service = new TransactionService(context);
            var success = await service.DeleteTransactionAsync(transactionId, userId);

            Assert.True(success);

            var account = await context.Accounts.FindAsync(accountId);
            Assert.NotNull(account);
            Assert.Equal(1000, account.Balance);
        }
    }

    [Fact]
    public async Task DeleteTransactionAsync_Expense_RollsBackBalance()
    {
        var options = CreateInMemoryDbContextOptions();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        using (var context = new ApplicationDbContext(options))
        {
            context.Accounts.Add(new Account
            {
                Id = accountId,
                Name = "Test Account",
                Type = AccountType.Cash,
                Balance = 700,
                UserId = userId
            });
            context.Transactions.Add(new Transaction
            {
                Id = transactionId,
                Type = TransactionType.Expense,
                Amount = 300,
                TransactionDate = DateTime.UtcNow,
                AccountId = accountId,
                UserId = userId
            });
            await context.SaveChangesAsync();
        }

        using (var context = new ApplicationDbContext(options))
        {
            var service = new TransactionService(context);
            var success = await service.DeleteTransactionAsync(transactionId, userId);

            Assert.True(success);

            var account = await context.Accounts.FindAsync(accountId);
            Assert.NotNull(account);
            Assert.Equal(1000, account.Balance);
        }
    }

    [Fact]
    public async Task UpdateTransactionAsync_ChangesBalanceCorrectly()
    {
        var options = CreateInMemoryDbContextOptions();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        using (var context = new ApplicationDbContext(options))
        {
            context.Accounts.Add(new Account
            {
                Id = accountId,
                Name = "Test Account",
                Type = AccountType.Cash,
                Balance = 1500,
                UserId = userId
            });
            context.Transactions.Add(new Transaction
            {
                Id = transactionId,
                Type = TransactionType.Income,
                Amount = 500,
                TransactionDate = DateTime.UtcNow,
                AccountId = accountId,
                UserId = userId
            });
            await context.SaveChangesAsync();
        }

        using (var context = new ApplicationDbContext(options))
        {
            var service = new TransactionService(context);
            var dto = new UpdateTransactionDto(
                TransactionType.Expense,
                200,
                DateTime.UtcNow,
                "Updated Transaction",
                null,
                accountId,
                null,
                null
            );

            var success = await service.UpdateTransactionAsync(transactionId, dto, userId);

            Assert.True(success);

            var account = await context.Accounts.FindAsync(accountId);
            Assert.NotNull(account);
            Assert.Equal(800, account.Balance);
        }
    }
}
