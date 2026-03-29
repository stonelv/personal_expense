using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _context;

    public TransactionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TransactionResponseDto>> GetTransactionsAsync(Guid userId, TransactionFilterDto filter)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .Where(t => t.UserId == userId);

        if (filter.Year.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Year == filter.Year.Value);
        }

        if (filter.Month.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Month == filter.Month.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TransactionResponseDto(
                t.Id,
                t.Type,
                t.Amount,
                t.TransactionDate,
                t.Description,
                t.AttachmentUrl,
                t.AccountId,
                t.Account.Name,
                t.CategoryId,
                t.Category?.Name,
                t.TransferToAccountId,
                t.TransferToAccount?.Name,
                t.CreatedAt,
                t.UpdatedAt
            ))
            .ToListAsync();

        return new PagedResult<TransactionResponseDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<TransactionResponseDto?> GetTransactionByIdAsync(Guid id, Guid userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return null;

        return new TransactionResponseDto(
            transaction.Id,
            transaction.Type,
            transaction.Amount,
            transaction.TransactionDate,
            transaction.Description,
            transaction.AttachmentUrl,
            transaction.AccountId,
            transaction.Account.Name,
            transaction.CategoryId,
            transaction.Category?.Name,
            transaction.TransferToAccountId,
            transaction.TransferToAccount?.Name,
            transaction.CreatedAt,
            transaction.UpdatedAt
        );
    }

    public async Task<TransactionResponseDto> CreateTransactionAsync(TransactionCreateDto transactionDto, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var newTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Type = transactionDto.Type,
                Amount = transactionDto.Amount,
                TransactionDate = transactionDto.TransactionDate,
                Description = transactionDto.Description,
                AttachmentUrl = transactionDto.AttachmentUrl,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                AccountId = transactionDto.AccountId,
                CategoryId = transactionDto.CategoryId,
                TransferToAccountId = transactionDto.TransferToAccountId
            };

            _context.Transactions.Add(newTransaction);

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transactionDto.AccountId && a.UserId == userId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            switch (transactionDto.Type)
            {
                case TransactionType.Income:
                    account.Balance += transactionDto.Amount;
                    break;
                case TransactionType.Expense:
                    account.Balance -= transactionDto.Amount;
                    break;
                case TransactionType.Transfer when transactionDto.TransferToAccountId.HasValue:
                    var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transactionDto.TransferToAccountId.Value && a.UserId == userId);
                    if (toAccount == null)
                    {
                        throw new InvalidOperationException("Transfer to account not found");
                    }
                    account.Balance -= transactionDto.Amount;
                    toAccount.Balance += transactionDto.Amount;
                    break;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Load related entities for response
            await _context.Entry(newTransaction)
                .Reference(t => t.Account)
                .LoadAsync();
            await _context.Entry(newTransaction)
                .Reference(t => t.Category)
                .LoadAsync();
            if (newTransaction.TransferToAccountId.HasValue)
            {
                await _context.Entry(newTransaction)
                    .Reference(t => t.TransferToAccount)
                    .LoadAsync();
            }

            return new TransactionResponseDto(
                newTransaction.Id,
                newTransaction.Type,
                newTransaction.Amount,
                newTransaction.TransactionDate,
                newTransaction.Description,
                newTransaction.AttachmentUrl,
                newTransaction.AccountId,
                newTransaction.Account.Name,
                newTransaction.CategoryId,
                newTransaction.Category?.Name,
                newTransaction.TransferToAccountId,
                newTransaction.TransferToAccount?.Name,
                newTransaction.CreatedAt,
                newTransaction.UpdatedAt
            );
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TransactionResponseDto> UpdateTransactionAsync(Guid id, TransactionUpdateDto transactionDto, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                throw new InvalidOperationException("Transaction not found");
            }

            // Revert previous transaction effect
            var oldAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            if (oldAccount != null)
            {
                switch (existingTransaction.Type)
                {
                    case TransactionType.Income:
                        oldAccount.Balance -= existingTransaction.Amount;
                        break;
                    case TransactionType.Expense:
                        oldAccount.Balance += existingTransaction.Amount;
                        break;
                    case TransactionType.Transfer when existingTransaction.TransferToAccountId.HasValue:
                        var oldToAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.TransferToAccountId.Value && a.UserId == userId);
                        if (oldToAccount != null)
                        {
                            oldAccount.Balance += existingTransaction.Amount;
                            oldToAccount.Balance -= existingTransaction.Amount;
                        }
                        break;
                }
            }

            // Update transaction properties
            existingTransaction.Type = transactionDto.Type;
            existingTransaction.Amount = transactionDto.Amount;
            existingTransaction.TransactionDate = transactionDto.TransactionDate;
            existingTransaction.Description = transactionDto.Description;
            existingTransaction.AttachmentUrl = transactionDto.AttachmentUrl;
            existingTransaction.UpdatedAt = DateTime.UtcNow;
            existingTransaction.AccountId = transactionDto.AccountId;
            existingTransaction.CategoryId = transactionDto.CategoryId;
            existingTransaction.TransferToAccountId = transactionDto.TransferToAccountId;

            // Apply new transaction effect
            var newAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transactionDto.AccountId && a.UserId == userId);
            if (newAccount == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            switch (transactionDto.Type)
            {
                case TransactionType.Income:
                    newAccount.Balance += transactionDto.Amount;
                    break;
                case TransactionType.Expense:
                    newAccount.Balance -= transactionDto.Amount;
                    break;
                case TransactionType.Transfer when transactionDto.TransferToAccountId.HasValue:
                    var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transactionDto.TransferToAccountId.Value && a.UserId == userId);
                    if (toAccount == null)
                    {
                        throw new InvalidOperationException("Transfer to account not found");
                    }
                    newAccount.Balance -= transactionDto.Amount;
                    toAccount.Balance += transactionDto.Amount;
                    break;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Load related entities for response
            await _context.Entry(existingTransaction)
                .Reference(t => t.Account)
                .LoadAsync();
            await _context.Entry(existingTransaction)
                .Reference(t => t.Category)
                .LoadAsync();
            if (existingTransaction.TransferToAccountId.HasValue)
            {
                await _context.Entry(existingTransaction)
                    .Reference(t => t.TransferToAccount)
                    .LoadAsync();
            }

            return new TransactionResponseDto(
                existingTransaction.Id,
                existingTransaction.Type,
                existingTransaction.Amount,
                existingTransaction.TransactionDate,
                existingTransaction.Description,
                existingTransaction.AttachmentUrl,
                existingTransaction.AccountId,
                existingTransaction.Account.Name,
                existingTransaction.CategoryId,
                existingTransaction.Category?.Name,
                existingTransaction.TransferToAccountId,
                existingTransaction.TransferToAccount?.Name,
                existingTransaction.CreatedAt,
                existingTransaction.UpdatedAt
            );
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteTransactionAsync(Guid id, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                throw new InvalidOperationException("Transaction not found");
            }

            // Revert transaction effect
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            if (account != null)
            {
                switch (existingTransaction.Type)
                {
                    case TransactionType.Income:
                        account.Balance -= existingTransaction.Amount;
                        break;
                    case TransactionType.Expense:
                        account.Balance += existingTransaction.Amount;
                        break;
                    case TransactionType.Transfer when existingTransaction.TransferToAccountId.HasValue:
                        var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.TransferToAccountId.Value && a.UserId == userId);
                        if (toAccount != null)
                        {
                            account.Balance += existingTransaction.Amount;
                            toAccount.Balance -= existingTransaction.Amount;
                        }
                        break;
                }
            }

            _context.Transactions.Remove(existingTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
