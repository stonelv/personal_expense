using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionFilterParams filter);
    Task<TransactionDto?> GetTransactionByIdAsync(Guid id, Guid userId);
    Task<TransactionDto> CreateTransactionAsync(TransactionCreateDto dto, Guid userId);
    Task<TransactionBudgetAlertDto> CreateTransactionWithBudgetCheckAsync(TransactionCreateDto dto, Guid userId, TimeZoneInfo? timeZone = null);
    Task<TransactionDto> UpdateTransactionAsync(Guid id, TransactionUpdateDto dto, Guid userId);
    Task<TransactionBudgetAlertDto> UpdateTransactionWithBudgetCheckAsync(Guid id, TransactionUpdateDto dto, Guid userId, TimeZoneInfo? timeZone = null);
    Task DeleteTransactionAsync(Guid id, Guid userId);
    Task<BudgetAlertDto?> DeleteTransactionWithBudgetCheckAsync(Guid id, Guid userId, TimeZoneInfo? timeZone = null);
}
