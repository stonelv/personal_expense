using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionFilterParams filter);
    Task<TransactionDto?> GetTransactionByIdAsync(Guid id, Guid userId);
    Task<TransactionDto> CreateTransactionAsync(TransactionCreateDto dto, Guid userId);
    Task<TransactionDto> UpdateTransactionAsync(Guid id, TransactionUpdateDto dto, Guid userId);
    Task DeleteTransactionAsync(Guid id, Guid userId);
    Task<TransferResultDto> CreateTransferAsync(TransferCreateDto dto, Guid userId);
    Task<AccountBalanceHistoryDto> GetAccountBalanceHistoryAsync(Guid accountId, Guid userId, DateTime? startDate, DateTime? endDate);
}
