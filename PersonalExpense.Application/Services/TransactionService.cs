using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetService _budgetService;

    public TransactionService(ApplicationDbContext context, IBudgetService budgetService)
    {
        _context = context;
        _budgetService = budgetService;
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionFilterParams filter)
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

        if (filter.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == filter.AccountId.Value || 
                                      t.TransferToAccountId == filter.AccountId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= filter.EndDate.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<TransactionDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid id, Guid userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        return transaction != null ? MapToDto(transaction) : null;
    }

    public async Task<TransactionBudgetAlertDto> CreateTransactionAsync(TransactionCreateDto dto, Guid userId, TimeZoneInfo? timeZone = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);
            
            if (account == null)
            {
                throw new BadRequestException("Account not found");
            }

            if (dto.Type == TransactionType.Transfer && !dto.TransferToAccountId.HasValue)
            {
                throw new BadRequestException("TransferToAccountId is required for transfer transactions");
            }

            if (dto.Type == TransactionType.Transfer && dto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == dto.TransferToAccountId.Value && a.UserId == userId);
                
                if (toAccount == null)
                {
                    throw new BadRequestException("Transfer to account not found");
                }
            }

            var newTransaction = new Transaction
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

            await ApplyTransactionEffectsAsync(newTransaction, account, userId, isCreate: true);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var transactionDto = await GetTransactionByIdAsync(newTransaction.Id, userId) 
                ?? throw new NotFoundException(nameof(Transaction), newTransaction.Id);

            var budgetAlert = await _budgetService.CheckBudgetAlertAfterTransactionAsync(
                userId, 
                dto.Type, 
                dto.TransactionDate, 
                timeZone);

            return new TransactionBudgetAlertDto(transactionDto, budgetAlert);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TransactionBudgetAlertDto> UpdateTransactionAsync(Guid id, TransactionUpdateDto dto, Guid userId, TimeZoneInfo? timeZone = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                throw new NotFoundException(nameof(Transaction), id);
            }

            var newAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);
            
            if (newAccount == null)
            {
                throw new BadRequestException("Account not found");
            }

            if (dto.Type == TransactionType.Transfer && !dto.TransferToAccountId.HasValue)
            {
                throw new BadRequestException("TransferToAccountId is required for transfer transactions");
            }

            if (dto.Type == TransactionType.Transfer && dto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == dto.TransferToAccountId.Value && a.UserId == userId);
                
                if (toAccount == null)
                {
                    throw new BadRequestException("Transfer to account not found");
                }
            }

            var oldAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            
            if (oldAccount != null)
            {
                await ApplyTransactionEffectsAsync(existingTransaction, oldAccount, userId, isCreate: false);
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

            await ApplyTransactionEffectsAsync(existingTransaction, newAccount, userId, isCreate: true);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var transactionDto = await GetTransactionByIdAsync(existingTransaction.Id, userId) 
                ?? throw new NotFoundException(nameof(Transaction), existingTransaction.Id);

            var budgetAlert = await _budgetService.CheckBudgetAlertAfterTransactionAsync(
                userId, 
                dto.Type, 
                dto.TransactionDate, 
                timeZone);

            return new TransactionBudgetAlertDto(transactionDto, budgetAlert);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BudgetAlertDto?> DeleteTransactionAsync(Guid id, Guid userId, TimeZoneInfo? timeZone = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                throw new NotFoundException(nameof(Transaction), id);
            }

            var transactionType = existingTransaction.Type;
            var transactionDate = existingTransaction.TransactionDate;

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            
            if (account != null)
            {
                await ApplyTransactionEffectsAsync(existingTransaction, account, userId, isCreate: false);
            }

            _context.Transactions.Remove(existingTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var budgetAlert = await _budgetService.CheckBudgetAlertAfterTransactionAsync(
                userId, 
                transactionType, 
                transactionDate, 
                timeZone);

            return budgetAlert;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ApplyTransactionEffectsAsync(Transaction transaction, Account account, Guid userId, bool isCreate)
    {
        var multiplier = isCreate ? 1 : -1;

        switch (transaction.Type)
        {
            case TransactionType.Income:
                account.Balance += transaction.Amount * multiplier;
                break;
            
            case TransactionType.Expense:
                account.Balance -= transaction.Amount * multiplier;
                break;
            
            case TransactionType.Transfer:
                if (!transaction.TransferToAccountId.HasValue)
                {
                    throw new BadRequestException("TransferToAccountId is required for transfer transactions");
                }

                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == transaction.TransferToAccountId.Value && a.UserId == userId);
                
                if (toAccount == null)
                {
                    throw new BadRequestException("Transfer to account not found");
                }

                account.Balance -= transaction.Amount * multiplier;
                toAccount.Balance += transaction.Amount * multiplier;
                break;
        }
    }

    private static TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto(
            transaction.Id,
            transaction.Type,
            transaction.Amount,
            transaction.TransactionDate,
            transaction.Description,
            transaction.AttachmentUrl,
            transaction.CreatedAt,
            transaction.UpdatedAt,
            transaction.AccountId,
            transaction.Account?.Name,
            transaction.CategoryId,
            transaction.Category?.Name,
            transaction.TransferToAccountId,
            transaction.TransferToAccount?.Name
        );
    }
}
