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

    public TransactionService(ApplicationDbContext context)
    {
        _context = context;
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

    public async Task<TransactionDto> CreateTransactionAsync(TransactionCreateDto dto, Guid userId)
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

            return await GetTransactionByIdAsync(newTransaction.Id, userId) 
                ?? throw new NotFoundException(nameof(Transaction), newTransaction.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TransferResultDto> CreateTransferAsync(TransferCreateDto dto, Guid userId)
    {
        if (dto.Amount <= 0)
        {
            throw new BadRequestException("Transfer amount must be greater than 0");
        }

        if (dto.FromAccountId == dto.ToAccountId)
        {
            throw new BadRequestException("Cannot transfer to the same account");
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var fromAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == dto.FromAccountId && a.UserId == userId);
            
            if (fromAccount == null)
            {
                throw new BadRequestException("From account not found");
            }

            var toAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == dto.ToAccountId && a.UserId == userId);
            
            if (toAccount == null)
            {
                throw new BadRequestException("To account not found");
            }

            if (fromAccount.Balance < dto.Amount)
            {
                throw new BadRequestException("Insufficient balance in from account");
            }

            var outgoingTransactionId = Guid.NewGuid();
            var incomingTransactionId = Guid.NewGuid();

            var outgoingTransaction = new Transaction
            {
                Id = outgoingTransactionId,
                Type = TransactionType.Expense,
                Amount = dto.Amount,
                TransactionDate = dto.TransactionDate,
                Description = $"Transfer to {toAccount.Name}: {dto.Description ?? string.Empty}".Trim(),
                AttachmentUrl = dto.AttachmentUrl,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                AccountId = dto.FromAccountId,
                CategoryId = null,
                TransferToAccountId = dto.ToAccountId,
                RelatedTransactionId = incomingTransactionId
            };

            var incomingTransaction = new Transaction
            {
                Id = incomingTransactionId,
                Type = TransactionType.Income,
                Amount = dto.Amount,
                TransactionDate = dto.TransactionDate,
                Description = $"Transfer from {fromAccount.Name}: {dto.Description ?? string.Empty}".Trim(),
                AttachmentUrl = dto.AttachmentUrl,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                AccountId = dto.ToAccountId,
                CategoryId = null,
                TransferToAccountId = dto.FromAccountId,
                RelatedTransactionId = outgoingTransactionId
            };

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;
            fromAccount.UpdatedAt = DateTime.UtcNow;
            toAccount.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Add(outgoingTransaction);
            _context.Transactions.Add(incomingTransaction);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            var outgoingDto = await GetTransactionByIdAsync(outgoingTransactionId, userId)
                ?? throw new NotFoundException(nameof(Transaction), outgoingTransactionId);
            
            var incomingDto = await GetTransactionByIdAsync(incomingTransactionId, userId)
                ?? throw new NotFoundException(nameof(Transaction), incomingTransactionId);

            return new TransferResultDto(outgoingDto, incomingDto);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TransactionDto> UpdateTransactionAsync(Guid id, TransactionUpdateDto dto, Guid userId)
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

            if (existingTransaction.RelatedTransactionId.HasValue)
            {
                throw new BadRequestException("Cannot update transfer transaction directly. Delete and recreate it instead.");
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

            return await GetTransactionByIdAsync(existingTransaction.Id, userId) 
                ?? throw new NotFoundException(nameof(Transaction), existingTransaction.Id);
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
                throw new NotFoundException(nameof(Transaction), id);
            }

            if (existingTransaction.RelatedTransactionId.HasValue)
            {
                var relatedTransaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == existingTransaction.RelatedTransactionId.Value && t.UserId == userId);
                
                if (relatedTransaction != null)
                {
                    var fromAccount = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
                    
                    var toAccount = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.Id == relatedTransaction.AccountId && a.UserId == userId);
                    
                    if (existingTransaction.Type == TransactionType.Expense && fromAccount != null)
                    {
                        fromAccount.Balance += existingTransaction.Amount;
                        fromAccount.UpdatedAt = DateTime.UtcNow;
                    }
                    
                    if (relatedTransaction.Type == TransactionType.Income && toAccount != null)
                    {
                        toAccount.Balance -= relatedTransaction.Amount;
                        toAccount.UpdatedAt = DateTime.UtcNow;
                    }

                    _context.Transactions.Remove(relatedTransaction);
                }
            }
            else
            {
                var account = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
                
                if (account != null)
                {
                    await ApplyTransactionEffectsAsync(existingTransaction, account, userId, isCreate: false);
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

    public async Task<AccountBalanceHistoryDto> GetAccountBalanceHistoryAsync(Guid accountId, Guid userId, DateTime? startDate, DateTime? endDate)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
        
        if (account == null)
        {
            throw new NotFoundException(nameof(Account), accountId);
        }

        var query = _context.Transactions
            .Include(t => t.RelatedTransaction)
            .Where(t => t.UserId == userId && t.AccountId == accountId);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        }

        var transactions = await query
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);
        
        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var netChange = totalIncome - totalExpense;
        
        var startingBalance = account.Balance - netChange;

        var balanceHistory = new List<BalanceEntryDto>();
        var currentBalance = startingBalance;

        foreach (var trans in transactions)
        {
            if (trans.Type == TransactionType.Income)
            {
                currentBalance += trans.Amount;
            }
            else if (trans.Type == TransactionType.Expense)
            {
                currentBalance -= trans.Amount;
            }

            balanceHistory.Add(new BalanceEntryDto(
                TransactionDate: trans.TransactionDate,
                Description: trans.Description,
                TransactionType: trans.Type,
                Amount: trans.Amount,
                BalanceAfterTransaction: currentBalance,
                RelatedTransactionId: trans.RelatedTransactionId
            ));
        }

        return new AccountBalanceHistoryDto(
            AccountId: account.Id,
            AccountName: account.Name,
            AccountType: account.Type,
            BalanceHistory: balanceHistory,
            StartingBalance: startingBalance,
            EndingBalance: account.Balance,
            TotalIncome: totalIncome,
            TotalExpense: totalExpense,
            NetChange: netChange
        );
    }

    private async Task ApplyTransactionEffectsAsync(Transaction transaction, Account account, Guid userId, bool isCreate)
    {
        var multiplier = isCreate ? 1 : -1;

        switch (transaction.Type)
        {
            case TransactionType.Income:
                account.Balance += transaction.Amount * multiplier;
                account.UpdatedAt = DateTime.UtcNow;
                break;
            
            case TransactionType.Expense:
                account.Balance -= transaction.Amount * multiplier;
                account.UpdatedAt = DateTime.UtcNow;
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
                account.UpdatedAt = DateTime.UtcNow;
                toAccount.Balance += transaction.Amount * multiplier;
                toAccount.UpdatedAt = DateTime.UtcNow;
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
            transaction.TransferToAccount?.Name,
            transaction.RelatedTransactionId
        );
    }
}
