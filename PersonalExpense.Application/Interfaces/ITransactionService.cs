using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface ITransactionService
{
    Task<PagedResult<TransactionResponseDto>> GetTransactionsAsync(Guid userId, TransactionFilterDto filter);
    Task<TransactionResponseDto?> GetTransactionByIdAsync(Guid id, Guid userId);
    Task<TransactionResponseDto> CreateTransactionAsync(TransactionCreateDto transactionDto, Guid userId);
    Task<TransactionResponseDto> UpdateTransactionAsync(Guid id, TransactionUpdateDto transactionDto, Guid userId);
    Task DeleteTransactionAsync(Guid id, Guid userId);
}
