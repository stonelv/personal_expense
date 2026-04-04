using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _context;

    public TransactionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponseDto<TransactionResponseDto>> GetTransactionsAsync(TransactionFilterDto filter, Guid userId)
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
                t.CreatedAt,
                t.UpdatedAt,
                t.AccountId,
                t.Account.Name,
                t.CategoryId,
                t.Category?.Name,
                t.TransferToAccountId,
                t.TransferToAccount?.Name
            ))
            .ToListAsync();

        return new PagedResponseDto<TransactionResponseDto>(
            items,
            totalCount,
            filter.PageNumber,
            filter.PageSize,
            filter.PageNumber * filter.PageSize < totalCount,
            filter.PageNumber > 1
        );
    }

    public async Task<TransactionResponseDto?> GetTransactionAsync(Guid id, Guid userId)
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
            transaction.CreatedAt,
            transaction.UpdatedAt,
            transaction.AccountId,
            transaction.Account.Name,
            transaction.CategoryId,
            transaction.Category?.Name,
            transaction.TransferToAccountId,
            transaction.TransferToAccount?.Name
        );
    }

    public async Task<TransactionResponseDto> CreateTransactionAsync(CreateTransactionDto dto, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var newTransaction = new Domain.Entities.Transaction
            {
                Id = Guid.NewGuid(),
                Type = dto.Type,
                Amount = dto.Amount,
                TransactionDate = dto.TransactionDate,
                Description = dto.Description,
                AttachmentUrl = dto.AttachmentUrl,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                AccountId = dto.AccountId,
                CategoryId = dto.CategoryId,
                TransferToAccountId = dto.TransferToAccountId
            };

            _context.Transactions.Add(newTransaction);

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);
            if (account == null)
            {
                throw new ArgumentException("Account not found");
            }

            if (dto.Type == Domain.Entities.TransactionType.Income)
            {
                account.Balance += dto.Amount;
            }
            else if (dto.Type == Domain.Entities.TransactionType.Expense)
            {
                account.Balance -= dto.Amount;
            }
            else if (dto.Type == Domain.Entities.TransactionType.Transfer && dto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == dto.TransferToAccountId.Value && a.UserId == userId);
                if (toAccount == null)
                {
                    throw new ArgumentException("Transfer to account not found");
                }
                account.Balance -= dto.Amount;
                toAccount.Balance += dto.Amount;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetTransactionAsync(newTransaction.Id, userId) ?? throw new InvalidOperationException("Transaction not found after creation");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateTransactionAsync(Guid id, UpdateTransactionDto dto, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
                return false;

            var oldAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            if (oldAccount != null)
            {
                if (existingTransaction.Type == Domain.Entities.TransactionType.Income)
                {
                    oldAccount.Balance -= existingTransaction.Amount;
                }
                else if (existingTransaction.Type == Domain.Entities.TransactionType.Expense)
                {
                    oldAccount.Balance += existingTransaction.Amount;
                }
                else if (existingTransaction.Type == Domain.Entities.TransactionType.Transfer && existingTransaction.TransferToAccountId.HasValue)
                {
                    var oldToAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.TransferToAccountId.Value && a.UserId == userId);
                    if (oldToAccount != null)
                    {
                        oldAccount.Balance += existingTransaction.Amount;
                        oldToAccount.Balance -= existingTransaction.Amount;
                    }
                }
            }

            existingTransaction.Type = dto.Type;
            existingTransaction.Amount = dto.Amount;
            existingTransaction.TransactionDate = dto.TransactionDate;
            existingTransaction.Description = dto.Description;
            existingTransaction.AttachmentUrl = dto.AttachmentUrl;
            existingTransaction.UpdatedAt = DateTime.UtcNow;
            existingTransaction.AccountId = dto.AccountId;
            existingTransaction.CategoryId = dto.CategoryId;
            existingTransaction.TransferToAccountId = dto.TransferToAccountId;

            var newAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);
            if (newAccount == null)
            {
                throw new ArgumentException("Account not found");
            }

            if (dto.Type == Domain.Entities.TransactionType.Income)
            {
                newAccount.Balance += dto.Amount;
            }
            else if (dto.Type == Domain.Entities.TransactionType.Expense)
            {
                newAccount.Balance -= dto.Amount;
            }
            else if (dto.Type == Domain.Entities.TransactionType.Transfer && dto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == dto.TransferToAccountId.Value && a.UserId == userId);
                if (toAccount == null)
                {
                    throw new ArgumentException("Transfer to account not found");
                }
                newAccount.Balance -= dto.Amount;
                toAccount.Balance += dto.Amount;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteTransactionAsync(Guid id, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
                return false;

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            if (account != null)
            {
                if (existingTransaction.Type == Domain.Entities.TransactionType.Income)
                {
                    account.Balance -= existingTransaction.Amount;
                }
                else if (existingTransaction.Type == Domain.Entities.TransactionType.Expense)
                {
                    account.Balance += existingTransaction.Amount;
                }
                else if (existingTransaction.Type == Domain.Entities.TransactionType.Transfer && existingTransaction.TransferToAccountId.HasValue)
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
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
