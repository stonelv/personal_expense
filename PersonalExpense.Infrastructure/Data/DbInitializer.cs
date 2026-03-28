using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        var defaultUserEmail = "demo@example.com";
        var defaultUserPassword = "Demo@123456";

        if (!userManager.Users.Any())
        {
            var user = new User
            {
                UserName = defaultUserEmail,
                Email = defaultUserEmail,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, defaultUserPassword);

            if (result.Succeeded)
            {
                await CreateDefaultCategoriesAsync(context, user.Id);
                await CreateDefaultAccountsAsync(context, user.Id);
            }
        }
    }

    private static async Task CreateDefaultCategoriesAsync(ApplicationDbContext context, Guid userId)
    {
        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "工资", Type = CategoryType.Income, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "奖金", Type = CategoryType.Income, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "投资收益", Type = CategoryType.Income, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "其他收入", Type = CategoryType.Income, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "餐饮", Type = CategoryType.Expense, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "交通", Type = CategoryType.Expense, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "购物", Type = CategoryType.Expense, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "娱乐", Type = CategoryType.Expense, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "医疗", Type = CategoryType.Expense, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "教育", Type = CategoryType.Expense, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "住房", Type = CategoryType.Expense, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "其他支出", Type = CategoryType.Expense, UserId = userId, CreatedAt = DateTime.UtcNow }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
    }

    private static async Task CreateDefaultAccountsAsync(ApplicationDbContext context, Guid userId)
    {
        var accounts = new List<Account>
        {
            new() { Id = Guid.NewGuid(), Name = "现金", Type = AccountType.Cash, Balance = 0, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "银行卡", Type = AccountType.BankCard, Balance = 0, UserId = userId, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "信用卡", Type = AccountType.CreditCard, Balance = 0, UserId = userId, CreatedAt = DateTime.UtcNow }
        };

        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();
    }
}
