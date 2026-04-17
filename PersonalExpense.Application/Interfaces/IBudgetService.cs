using PersonalExpense.Application.DTOs;
using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.Interfaces;

public interface IBudgetService
{
    Task<List<BudgetDto>> GetBudgetsAsync(Guid userId, int? year, int? month);
    Task<BudgetDto?> GetBudgetByIdAsync(Guid id, Guid userId);
    Task<BudgetStatusDto> GetBudgetStatusAsync(Guid userId, int year, int month);
    Task<BudgetStatusDto> GetBudgetStatusAsync(Guid userId, int year, int month, TimeZoneInfo timeZone);
    Task<BudgetAlertDto> GetBudgetAlertsAsync(Guid userId, int year, int month);
    Task<BudgetAlertDto> GetBudgetAlertsAsync(Guid userId, int year, int month, TimeZoneInfo timeZone);
    Task<BudgetAlertDto?> CheckBudgetAlertAfterTransactionAsync(
        Guid userId, 
        TransactionType transactionType, 
        DateTime transactionDate, 
        TimeZoneInfo? timeZone = null);
    Task<BudgetDto> CreateBudgetAsync(BudgetCreateDto dto, Guid userId);
    Task<BudgetDto> UpdateBudgetAsync(Guid id, BudgetUpdateDto dto, Guid userId);
    Task DeleteBudgetAsync(Guid id, Guid userId);
}
