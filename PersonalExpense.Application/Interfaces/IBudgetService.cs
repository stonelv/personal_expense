using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<BudgetResponseDto>> GetBudgetsAsync(Guid userId, int? year = null, int? month = null);
    Task<BudgetResponseDto?> GetBudgetAsync(Guid id, Guid userId);
    Task<BudgetStatusDto> GetBudgetStatusAsync(int year, int month, Guid userId);
    Task<BudgetResponseDto> CreateBudgetAsync(CreateBudgetDto dto, Guid userId);
    Task<bool> UpdateBudgetAsync(Guid id, UpdateBudgetDto dto, Guid userId);
    Task<bool> DeleteBudgetAsync(Guid id, Guid userId);
}
