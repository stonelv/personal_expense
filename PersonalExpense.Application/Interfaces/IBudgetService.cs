using PersonalExpense.Application.DTOs.Budget;

namespace PersonalExpense.Application.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<BudgetResponseDto>> GetBudgetsAsync(Guid userId, int? year, int? month);
    Task<BudgetResponseDto?> GetBudgetByIdAsync(Guid id, Guid userId);
    Task<BudgetStatusDto> GetBudgetStatusAsync(Guid userId, int year, int month);
    Task<BudgetResponseDto> CreateBudgetAsync(Guid userId, BudgetCreateDto dto);
    Task UpdateBudgetAsync(Guid id, Guid userId, BudgetUpdateDto dto);
    Task DeleteBudgetAsync(Guid id, Guid userId);
}
