using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Api.Services;

public interface ITransactionService
{
    Task ProcessTransactionAsync(Transaction transaction);
    Task ReverseTransactionAsync(Transaction transaction);
    Task UpdateTransactionAsync(Transaction oldTransaction, Transaction newTransaction);
}