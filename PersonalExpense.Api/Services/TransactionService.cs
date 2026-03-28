using PersonalExpense.Domain.Entities;
using PersonalExpense.Domain.Enums;
using PersonalExpense.Infrastructure.Repositories;

namespace PersonalExpense.Api.Services;

public class TransactionService : ITransactionService
{
    private readonly IAccountRepository _accountRepository;
    
    public TransactionService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }
    
    public async Task ProcessTransactionAsync(Transaction transaction)
    {
        var account = await _accountRepository.GetByIdAndUserIdAsync(transaction.AccountId, transaction.UserId);
        if (account == null)
        {
            throw new InvalidOperationException("Account not found");
        }
        
        switch (transaction.Type)
        {
            case TransactionType.Income:
                account.Balance += transaction.Amount;
                break;
                
            case TransactionType.Expense:
                if (account.Balance < transaction.Amount)
                {
                    throw new InvalidOperationException("Insufficient balance");
                }
                account.Balance -= transaction.Amount;
                break;
                
            case TransactionType.Transfer:
                if (!transaction.ToAccountId.HasValue)
                {
                    throw new InvalidOperationException("Transfer requires a destination account");
                }
                
                if (account.Balance < transaction.Amount)
                {
                    throw new InvalidOperationException("Insufficient balance for transfer");
                }
                
                var toAccount = await _accountRepository.GetByIdAndUserIdAsync(transaction.ToAccountId.Value, transaction.UserId);
                if (toAccount == null)
                {
                    throw new InvalidOperationException("Destination account not found");
                }
                
                account.Balance -= transaction.Amount;
                toAccount.Balance += transaction.Amount;
                
                account.UpdatedAt = DateTime.UtcNow;
                toAccount.UpdatedAt = DateTime.UtcNow;
                
                await _accountRepository.UpdateAsync(account);
                await _accountRepository.UpdateAsync(toAccount);
                await _accountRepository.SaveChangesAsync();
                return;
                
            default:
                throw new InvalidOperationException("Invalid transaction type");
        }
        
        account.UpdatedAt = DateTime.UtcNow;
        await _accountRepository.UpdateAsync(account);
        await _accountRepository.SaveChangesAsync();
    }
    
    public async Task ReverseTransactionAsync(Transaction transaction)
    {
        var account = await _accountRepository.GetByIdAndUserIdAsync(transaction.AccountId, transaction.UserId);
        if (account == null)
        {
            throw new InvalidOperationException("Account not found");
        }
        
        switch (transaction.Type)
        {
            case TransactionType.Income:
                if (account.Balance < transaction.Amount)
                {
                    throw new InvalidOperationException("Insufficient balance to reverse income");
                }
                account.Balance -= transaction.Amount;
                break;
                
            case TransactionType.Expense:
                account.Balance += transaction.Amount;
                break;
                
            case TransactionType.Transfer:
                if (!transaction.ToAccountId.HasValue)
                {
                    throw new InvalidOperationException("Transfer requires a destination account");
                }
                
                var toAccount = await _accountRepository.GetByIdAndUserIdAsync(transaction.ToAccountId.Value, transaction.UserId);
                if (toAccount == null)
                {
                    throw new InvalidOperationException("Destination account not found");
                }
                
                if (toAccount.Balance < transaction.Amount)
                {
                    throw new InvalidOperationException("Insufficient balance in destination account to reverse transfer");
                }
                
                account.Balance += transaction.Amount;
                toAccount.Balance -= transaction.Amount;
                
                account.UpdatedAt = DateTime.UtcNow;
                toAccount.UpdatedAt = DateTime.UtcNow;
                
                await _accountRepository.UpdateAsync(account);
                await _accountRepository.UpdateAsync(toAccount);
                await _accountRepository.SaveChangesAsync();
                return;
                
            default:
                throw new InvalidOperationException("Invalid transaction type");
        }
        
        account.UpdatedAt = DateTime.UtcNow;
        await _accountRepository.UpdateAsync(account);
        await _accountRepository.SaveChangesAsync();
    }
    
    public async Task UpdateTransactionAsync(Transaction oldTransaction, Transaction newTransaction)
    {
        // First reverse the old transaction
        await ReverseTransactionAsync(oldTransaction);
        
        // Then process the new transaction
        await ProcessTransactionAsync(newTransaction);
    }
}