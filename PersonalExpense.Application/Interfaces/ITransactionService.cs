using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface ITransactionService
{
    Task<PagedResponseDto<TransactionResponseDto>> GetTransactionsAsync(TransactionFilterDto filter, Guid userId);
    Task<TransactionResponseDto?> GetTransactionAsync(Guid id, Guid userId);
    Task<TransactionResponseDto> CreateTransactionAsync(CreateTransactionDto dto, Guid userId);
    Task<bool> UpdateTransactionAsync(Guid id, UpdateTransactionDto dto, Guid userId);
    Task<bool> DeleteTransactionAsync(Guid id, Guid userId);
}
