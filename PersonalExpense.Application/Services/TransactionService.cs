using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs.Common;
using PersonalExpense.Application.DTOs.Transaction;
using PersonalExpense.Application.Exceptions;
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

    public async Task<PagedResult<TransactionResponseDto>> GetTransactionsAsync(Guid userId, TransactionQueryParameters parameters)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .Where(t => t.UserId == userId);

        if (parameters.Year.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Year == parameters.Year.Value);
        }

        if (parameters.Month.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Month == parameters.Month.Value);
        }

        if (parameters.Type.HasValue)
        {
            query = query.Where(t => t.Type == parameters.Type.Value);
        }

        if (parameters.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == parameters.AccountId.Value);
        }

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == parameters.CategoryId.Value);
        }

        if (parameters.StartDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= parameters.StartDate.Value);
        }

        if (parameters.EndDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= parameters.EndDate.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                TransactionDate = t.TransactionDate,
                Description = t.Description,
                AttachmentUrl = t.AttachmentUrl,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                AccountId = t.AccountId,
                AccountName = t.Account.Name,
                CategoryId = t.CategoryId,
                CategoryName = t.Category != null ? t.Category.Name : null,
                TransferToAccountId = t.TransferToAccountId,
                TransferToAccountName = t.TransferToAccount != null ? t.TransferToAccount.Name : null
            })
            .ToListAsync();

        return new PagedResult<TransactionResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }

    public async Task<TransactionResponseDto?> GetTransactionByIdAsync(Guid id, Guid userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
        {
            return null;
        }

        return new TransactionResponseDto
        {
            Id = transaction.Id,
            Type = transaction.Type,
            Amount = transaction.Amount,
            TransactionDate = transaction.TransactionDate,
            Description = transaction.Description,
            AttachmentUrl = transaction.AttachmentUrl,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            AccountId = transaction.AccountId,
            AccountName = transaction.Account.Name,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category != null ? transaction.Category.Name : null,
            TransferToAccountId = transaction.TransferToAccountId,
            TransferToAccountName = transaction.TransferToAccount != null ? transaction.TransferToAccount.Name : null
        };
    }

    public async Task<TransactionResponseDto> CreateTransactionAsync(Guid userId, TransactionCreateDto dto)
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
                throw new BadRequestException("Account not found");
            }

            if (dto.Type == TransactionType.Income)
            {
                account.Balance += dto.Amount;
            }
            else if (dto.Type == TransactionType.Expense)
            {
                account.Balance -= dto.Amount;
            }
            else if (dto.Type == TransactionType.Transfer && dto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == dto.TransferToAccountId.Value && a.UserId == userId);
                if (toAccount == null)
                {
                    throw new BadRequestException("Transfer to account not found");
                }
                account.Balance -= dto.Amount;
                toAccount.Balance += dto.Amount;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetTransactionByIdAsync(newTransaction.Id, userId) ?? throw new NotFoundException("Transaction not found after creation");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateTransactionAsync(Guid id, Guid userId, TransactionUpdateDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                throw new NotFoundException("Transaction", id);
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
                throw new BadRequestException("Account not found");
            }

            if (dto.Type == TransactionType.Income)
            {
                newAccount.Balance += dto.Amount;
            }
            else if (dto.Type == TransactionType.Expense)
            {
                newAccount.Balance -= dto.Amount;
            }
            else if (dto.Type == TransactionType.Transfer && dto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == dto.TransferToAccountId.Value && a.UserId == userId);
                if (toAccount == null)
                {
                    throw new BadRequestException("Transfer to account not found");
                }
                newAccount.Balance -= dto.Amount;
                toAccount.Balance += dto.Amount;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
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
                throw new NotFoundException("Transaction", id);
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
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
