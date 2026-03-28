using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;
using System.Security.Claims;

namespace PersonalExpense.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TransactionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions(
        [FromQuery] int? year, 
        [FromQuery] int? month,
        [FromQuery] TransactionType? type)
    {
        var userId = GetCurrentUserId();
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

        return await query.OrderByDescending(t => t.TransactionDate).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Transaction>> GetTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        var transaction = await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        return transaction;
    }

    [HttpPost]
    public async Task<ActionResult<Transaction>> PostTransaction(TransactionDto transactionDto)
    {
        var userId = GetCurrentUserId();
        
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
                return BadRequest("Account not found");
            }

            if (transactionDto.Type == TransactionType.Income)
            {
                account.Balance += transactionDto.Amount;
            }
            else if (transactionDto.Type == TransactionType.Expense)
            {
                account.Balance -= transactionDto.Amount;
            }
            else if (transactionDto.Type == TransactionType.Transfer && transactionDto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transactionDto.TransferToAccountId.Value && a.UserId == userId);
                if (toAccount == null)
                {
                    return BadRequest("Transfer to account not found");
                }
                account.Balance -= transactionDto.Amount;
                toAccount.Balance += transactionDto.Amount;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetTransaction), new { id = newTransaction.Id }, newTransaction);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTransaction(Guid id, TransactionDto transactionDto)
    {
        var userId = GetCurrentUserId();
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                return NotFound();
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

            existingTransaction.Type = transactionDto.Type;
            existingTransaction.Amount = transactionDto.Amount;
            existingTransaction.TransactionDate = transactionDto.TransactionDate;
            existingTransaction.Description = transactionDto.Description;
            existingTransaction.AttachmentUrl = transactionDto.AttachmentUrl;
            existingTransaction.UpdatedAt = DateTime.UtcNow;
            existingTransaction.AccountId = transactionDto.AccountId;
            existingTransaction.CategoryId = transactionDto.CategoryId;
            existingTransaction.TransferToAccountId = transactionDto.TransferToAccountId;

            var newAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transactionDto.AccountId && a.UserId == userId);
            if (newAccount == null)
            {
                return BadRequest("Account not found");
            }

            if (transactionDto.Type == TransactionType.Income)
            {
                newAccount.Balance += transactionDto.Amount;
            }
            else if (transactionDto.Type == TransactionType.Expense)
            {
                newAccount.Balance -= transactionDto.Amount;
            }
            else if (transactionDto.Type == TransactionType.Transfer && transactionDto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == transactionDto.TransferToAccountId.Value && a.UserId == userId);
                if (toAccount == null)
                {
                    return BadRequest("Transfer to account not found");
                }
                newAccount.Balance -= transactionDto.Amount;
                toAccount.Balance += transactionDto.Amount;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                return NotFound();
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

            return NoContent();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

public record TransactionDto(
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string? Description,
    string? AttachmentUrl,
    Guid AccountId,
    Guid? CategoryId,
    Guid? TransferToAccountId
);
