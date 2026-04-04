using PersonalExpense.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PersonalExpense.Application.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<Transaction>> GetTransactionsAsync(Guid userId, int? year, int? month, TransactionType? type, int page = 1, int pageSize = 10);
    Task<Transaction> GetTransactionByIdAsync(Guid id, Guid userId);
    Task<Transaction> CreateTransactionAsync(Transaction transaction, Guid userId);
    Task UpdateTransactionAsync(Guid id, Transaction transaction, Guid userId);
    Task DeleteTransactionAsync(Guid id, Guid userId);
    Task<int> GetTotalCountAsync(Guid userId, int? year, int? month, TransactionType? type);
}
