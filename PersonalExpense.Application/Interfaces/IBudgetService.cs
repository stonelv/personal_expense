using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<BudgetResponseDto>> GetBudgetsAsync(Guid userId, int? year, int? month);
    Task<BudgetResponseDto?> GetBudgetByIdAsync(Guid id, Guid userId);
    Task<BudgetStatusDto> GetBudgetStatusAsync(Guid userId, int year, int month);
    Task<BudgetResponseDto> CreateBudgetAsync(BudgetCreateDto budgetDto, Guid userId);
    Task<BudgetResponseDto> UpdateBudgetAsync(Guid id, BudgetUpdateDto budgetDto, Guid userId);
    Task DeleteBudgetAsync(Guid id, Guid userId);
}
