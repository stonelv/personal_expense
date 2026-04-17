using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface IBudgetService
{
    Task<List<BudgetDto>> GetBudgetsAsync(Guid userId, int? year, int? month);
    Task<BudgetDto?> GetBudgetByIdAsync(Guid id, Guid userId);
    Task<BudgetStatusDto> GetBudgetStatusAsync(Guid userId, int year, int month);
    Task<BudgetDto> CreateBudgetAsync(BudgetCreateDto dto, Guid userId);
    Task<BudgetDto> UpdateBudgetAsync(Guid id, BudgetUpdateDto dto, Guid userId);
    Task DeleteBudgetAsync(Guid id, Guid userId);
}
