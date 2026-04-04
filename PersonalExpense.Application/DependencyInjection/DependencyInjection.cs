using Microsoft.Extensions.DependencyInjection;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Application.Services;

namespace PersonalExpense.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBudgetService, BudgetService>();

        return services;
    }
}
