using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionFilterParams filter);
    Task<TransactionDto?> GetTransactionByIdAsync(Guid id, Guid userId);
    Task<TransactionBudgetAlertDto> CreateTransactionAsync(TransactionCreateDto dto, Guid userId, TimeZoneInfo? timeZone = null);
    Task<TransactionBudgetAlertDto> UpdateTransactionAsync(Guid id, TransactionUpdateDto dto, Guid userId, TimeZoneInfo? timeZone = null);
    Task<BudgetAlertDto?> DeleteTransactionAsync(Guid id, Guid userId, TimeZoneInfo? timeZone = null);
}
