using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace PersonalExpense.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _context;

    public TransactionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(Guid userId, int? year, int? month, TransactionType? type, int page = 1, int pageSize = 10)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        if (year.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Month == month.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        var skip = (page - 1) * pageSize;
        return await query.OrderByDescending(t => t.TransactionDate)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Transaction> GetTransactionByIdAsync(Guid id, Guid userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
        {
            throw new KeyNotFoundException("Transaction not found");
        }

        return transaction;
    }

    public async Task<Transaction> CreateTransactionAsync(Transaction transaction, Guid userId)
    {
        // 检查是否使用InMemory数据库
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        if (isInMemory)
        {
            // InMemory数据库不支持事务，直接执行操作
            transaction.UserId = userId;
            transaction.CreatedAt = DateTime.UtcNow;

            _context.Transactions.Add(transaction);

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.AccountId && a.UserId == userId);
            if (account == null)
            {
                throw new KeyNotFoundException("Account not found");
            }

            if (transaction.Type == TransactionType.Income)
            {
                account.Balance += transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                account.Balance -= transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Transfer && transaction.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.TransferToAccountId.Value && a.UserId == userId);
                if (toAccount == null)
                {
                    throw new KeyNotFoundException("Transfer to account not found");
                }
                account.Balance -= transaction.Amount;
                toAccount.Balance += transaction.Amount;
            }

            await _context.SaveChangesAsync();
            return transaction;
        }
        else
        {
            // 其他数据库使用事务
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                transaction.UserId = userId;
                transaction.CreatedAt = DateTime.UtcNow;

                _context.Transactions.Add(transaction);

                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.AccountId && a.UserId == userId);
                if (account == null)
                {
                    throw new KeyNotFoundException("Account not found");
                }

                if (transaction.Type == TransactionType.Income)
                {
                    account.Balance += transaction.Amount;
                }
                else if (transaction.Type == TransactionType.Expense)
                {
                    account.Balance -= transaction.Amount;
                }
                else if (transaction.Type == TransactionType.Transfer && transaction.TransferToAccountId.HasValue)
                {
                    var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.TransferToAccountId.Value && a.UserId == userId);
                    if (toAccount == null)
                    {
                        throw new KeyNotFoundException("Transfer to account not found");
                    }
                    account.Balance -= transaction.Amount;
                    toAccount.Balance += transaction.Amount;
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return transaction;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task UpdateTransactionAsync(Guid id, Transaction transaction, Guid userId)
    {
        // 检查是否使用InMemory数据库
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        if (isInMemory)
        {
            // InMemory数据库不支持事务，直接执行操作
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                throw new KeyNotFoundException("Transaction not found");
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            if (account != null)
            {
                if (existingTransaction.Type == TransactionType.Income)
                {
                    account.Balance -= existingTransaction.Amount;
                }
                else if (existingTransaction.Type == TransactionType.Expense)
                {
                    account.Balance += existingTransaction.Amount;
                }
                else if (existingTransaction.Type == TransactionType.Transfer && existingTransaction.TransferToAccountId.HasValue)
                {
                    var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.TransferToAccountId.Value && a.UserId == userId);
                    if (toAccount != null)
                    {
                        account.Balance += existingTransaction.Amount;
                        toAccount.Balance -= existingTransaction.Amount;
                    }
                }
            }

            existingTransaction.Type = transaction.Type;
            existingTransaction.Amount = transaction.Amount;
            existingTransaction.TransactionDate = transaction.TransactionDate;
            existingTransaction.Description = transaction.Description;
            existingTransaction.AttachmentUrl = transaction.AttachmentUrl;
            existingTransaction.UpdatedAt = DateTime.UtcNow;
            existingTransaction.AccountId = transaction.AccountId;
            existingTransaction.CategoryId = transaction.CategoryId;
            existingTransaction.TransferToAccountId = transaction.TransferToAccountId;

            var newAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.AccountId && a.UserId == userId);
            if (newAccount == null)
            {
                throw new KeyNotFoundException("Account not found");
            }

            if (transaction.Type == TransactionType.Income)
            {
                newAccount.Balance += transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                newAccount.Balance -= transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Transfer && transaction.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.TransferToAccountId.Value && a.UserId == userId);
                if (toAccount == null)
                {
                    throw new KeyNotFoundException("Transfer to account not found");
                }
                newAccount.Balance -= transaction.Amount;
                toAccount.Balance += transaction.Amount;
            }

            await _context.SaveChangesAsync();
        }
        else
        {
            // 其他数据库使用事务
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingTransaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

                if (existingTransaction == null)
                {
                    throw new KeyNotFoundException("Transaction not found");
                }

                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
                if (account != null)
                {
                    if (existingTransaction.Type == TransactionType.Income)
                    {
                        account.Balance -= existingTransaction.Amount;
                    }
                    else if (existingTransaction.Type == TransactionType.Expense)
                    {
                        account.Balance += existingTransaction.Amount;
                    }
                    else if (existingTransaction.Type == TransactionType.Transfer && existingTransaction.TransferToAccountId.HasValue)
                    {
                        var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.TransferToAccountId.Value && a.UserId == userId);
                        if (toAccount != null)
                        {
                            account.Balance += existingTransaction.Amount;
                            toAccount.Balance -= existingTransaction.Amount;
                        }
                    }
                }

                existingTransaction.Type = transaction.Type;
                existingTransaction.Amount = transaction.Amount;
                existingTransaction.TransactionDate = transaction.TransactionDate;
                existingTransaction.Description = transaction.Description;
                existingTransaction.AttachmentUrl = transaction.AttachmentUrl;
                existingTransaction.UpdatedAt = DateTime.UtcNow;
                existingTransaction.AccountId = transaction.AccountId;
                existingTransaction.CategoryId = transaction.CategoryId;
                existingTransaction.TransferToAccountId = transaction.TransferToAccountId;

                var newAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.AccountId && a.UserId == userId);
                if (newAccount == null)
                {
                    throw new KeyNotFoundException("Account not found");
                }

                if (transaction.Type == TransactionType.Income)
                {
                    newAccount.Balance += transaction.Amount;
                }
                else if (transaction.Type == TransactionType.Expense)
                {
                    newAccount.Balance -= transaction.Amount;
                }
                else if (transaction.Type == TransactionType.Transfer && transaction.TransferToAccountId.HasValue)
                {
                    var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.TransferToAccountId.Value && a.UserId == userId);
                    if (toAccount == null)
                    {
                        throw new KeyNotFoundException("Transfer to account not found");
                    }
                    newAccount.Balance -= transaction.Amount;
                    toAccount.Balance += transaction.Amount;
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task DeleteTransactionAsync(Guid id, Guid userId)
    {
        // 检查是否使用InMemory数据库
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        if (isInMemory)
        {
            // InMemory数据库不支持事务，直接执行操作
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                throw new KeyNotFoundException("Transaction not found");
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            if (account != null)
            {
                if (existingTransaction.Type == TransactionType.Income)
                {
                    account.Balance -= existingTransaction.Amount;
                }
                else if (existingTransaction.Type == TransactionType.Expense)
                {
                    account.Balance += existingTransaction.Amount;
                }
                else if (existingTransaction.Type == TransactionType.Transfer && existingTransaction.TransferToAccountId.HasValue)
                {
                    var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.TransferToAccountId.Value && a.UserId == userId);
                    if (toAccount != null)
                    {
                        account.Balance += existingTransaction.Amount;
                        toAccount.Balance -= existingTransaction.Amount;
                    }
                }
            }

            _context.Transactions.Remove(existingTransaction);
            await _context.SaveChangesAsync();
        }
        else
        {
            // 其他数据库使用事务
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingTransaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

                if (existingTransaction == null)
                {
                    throw new KeyNotFoundException("Transaction not found");
                }

                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
                if (account != null)
                {
                    if (existingTransaction.Type == TransactionType.Income)
                    {
                        account.Balance -= existingTransaction.Amount;
                    }
                    else if (existingTransaction.Type == TransactionType.Expense)
                    {
                        account.Balance += existingTransaction.Amount;
                    }
                    else if (existingTransaction.Type == TransactionType.Transfer && existingTransaction.TransferToAccountId.HasValue)
                    {
                        var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.TransferToAccountId.Value && a.UserId == userId);
                        if (toAccount != null)
                        {
                            account.Balance += existingTransaction.Amount;
                            toAccount.Balance -= existingTransaction.Amount;
                        }
                    }
                }

                _context.Transactions.Remove(existingTransaction);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task<int> GetTotalCountAsync(Guid userId, int? year, int? month, TransactionType? type)
    {
        var query = _context.Transactions.Where(t => t.UserId == userId);

        if (year.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Month == month.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        return await query.CountAsync();
    }
}
