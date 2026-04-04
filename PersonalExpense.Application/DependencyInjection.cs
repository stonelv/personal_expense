using Microsoft.Extensions.DependencyInjection;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Application.Services;

namespace PersonalExpense.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
