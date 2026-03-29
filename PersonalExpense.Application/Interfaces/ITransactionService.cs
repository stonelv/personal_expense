using PersonalExpense.Application.DTOs.Common;
using PersonalExpense.Application.DTOs.Transaction;

namespace PersonalExpense.Application.Interfaces;

public interface ITransactionService
{
    Task<PagedResult<TransactionResponseDto>> GetTransactionsAsync(Guid userId, TransactionQueryParameters parameters);
    Task<TransactionResponseDto?> GetTransactionByIdAsync(Guid id, Guid userId);
    Task<TransactionResponseDto> CreateTransactionAsync(Guid userId, TransactionCreateDto dto);
    Task UpdateTransactionAsync(Guid id, Guid userId, TransactionUpdateDto dto);
    Task DeleteTransactionAsync(Guid id, Guid userId);
}
