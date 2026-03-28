using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Api.DTOs.Transaction;
using PersonalExpense.Api.Extensions;
using PersonalExpense.Api.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Domain.Enums;
using PersonalExpense.Infrastructure.Repositories;

namespace PersonalExpense.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionService _transactionService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    
    public TransactionsController(
        ITransactionRepository transactionRepository,
        ITransactionService transactionService,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository)
    {
        _transactionRepository = transactionRepository;
        _transactionService = transactionService;
        _categoryRepository = categoryRepository;
        _accountRepository = accountRepository;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] TransactionType? type,
        [FromQuery] int? categoryId)
    {
        var userId = this.GetCurrentUserId();
        IEnumerable<Transaction> transactions;
        
        if (year.HasValue && month.HasValue)
        {
            transactions = await _transactionRepository.GetByMonthAndUserIdAsync(year.Value, month.Value, userId);
        }
        else if (type.HasValue)
        {
            transactions = await _transactionRepository.GetByTypeAndUserIdAsync(type.Value, userId);
        }
        else if (categoryId.HasValue)
        {
            transactions = await _transactionRepository.GetByCategoryAndUserIdAsync(categoryId.Value, userId);
        }
        else
        {
            transactions = await _transactionRepository.GetAllByUserIdAsync(userId);
        }
        
        var transactionDtos = transactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            Amount = t.Amount,
            Type = t.Type,
            Date = t.Date,
            Note = t.Note,
            AttachmentUrl = t.AttachmentUrl,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            CategoryId = t.CategoryId,
            CategoryName = t.Category?.Name,
            AccountId = t.AccountId,
            AccountName = t.Account?.Name ?? string.Empty,
            ToAccountId = t.ToAccountId,
            ToAccountName = t.ToAccount?.Name
        }).ToList();
        
        return Ok(transactionDtos);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = this.GetCurrentUserId();
        var transaction = await _transactionRepository.GetByIdAndUserIdAsync(id, userId);
        
        if (transaction == null)
        {
            return NotFound();
        }
        
        var transactionDto = new TransactionDto
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Date = transaction.Date,
            Note = transaction.Note,
            AttachmentUrl = transaction.AttachmentUrl,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name,
            AccountId = transaction.AccountId,
            AccountName = transaction.Account?.Name ?? string.Empty,
            ToAccountId = transaction.ToAccountId,
            ToAccountName = transaction.ToAccount?.Name
        };
        
        return Ok(transactionDto);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateTransactionRequest request)
    {
        var userId = this.GetCurrentUserId();
        
        // Validate transaction type requirements
        if (request.Type == TransactionType.Transfer)
        {
            if (!request.ToAccountId.HasValue)
            {
                return BadRequest("Transfer transactions require a ToAccountId");
            }
            
            if (request.AccountId == request.ToAccountId)
            {
                return BadRequest("Source and destination accounts must be different");
            }
        }
        else
        {
            if (!request.CategoryId.HasValue)
            {
                return BadRequest("Income and Expense transactions require a CategoryId");
            }
            
            // Validate category exists and belongs to user
            var category = await _categoryRepository.GetByIdAndUserIdAsync(request.CategoryId.Value, userId);
            if (category == null)
            {
                return NotFound("Category not found");
            }
            
            // Validate category type matches transaction type
            if ((request.Type == TransactionType.Income && category.Type != CategoryType.Income) ||
                (request.Type == TransactionType.Expense && category.Type != CategoryType.Expense))
            {
                return BadRequest("Category type does not match transaction type");
            }
        }
        
        // Validate account exists and belongs to user
        var account = await _accountRepository.GetByIdAndUserIdAsync(request.AccountId, userId);
        if (account == null)
        {
            return NotFound("Account not found");
        }
        
        // Validate destination account if transfer
        if (request.Type == TransactionType.Transfer && request.ToAccountId.HasValue)
        {
            var toAccount = await _accountRepository.GetByIdAndUserIdAsync(request.ToAccountId.Value, userId);
            if (toAccount == null)
            {
                return NotFound("Destination account not found");
            }
        }
        
        var transaction = new Transaction
        {
            Amount = request.Amount,
            Type = request.Type,
            Date = request.Date ?? DateTime.UtcNow,
            Note = request.Note,
            AttachmentUrl = request.AttachmentUrl,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            ToAccountId = request.ToAccountId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        try
        {
            await _transactionService.ProcessTransactionAsync(transaction);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        
        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();
        
        // Reload with navigation properties
        transaction = await _transactionRepository.GetByIdAndUserIdAsync(transaction.Id, userId);
        
        var transactionDto = new TransactionDto
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Date = transaction.Date,
            Note = transaction.Note,
            AttachmentUrl = transaction.AttachmentUrl,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name,
            AccountId = transaction.AccountId,
            AccountName = transaction.Account?.Name ?? string.Empty,
            ToAccountId = transaction.ToAccountId,
            ToAccountName = transaction.ToAccount?.Name
        };
        
        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transactionDto);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateTransactionRequest request)
    {
        var userId = this.GetCurrentUserId();
        var oldTransaction = await _transactionRepository.GetByIdAndUserIdAsync(id, userId);
        
        if (oldTransaction == null)
        {
            return NotFound();
        }
        
        // Validate transaction type requirements
        if (request.Type == TransactionType.Transfer)
        {
            if (!request.ToAccountId.HasValue)
            {
                return BadRequest("Transfer transactions require a ToAccountId");
            }
            
            if (request.AccountId == request.ToAccountId)
            {
                return BadRequest("Source and destination accounts must be different");
            }
        }
        else
        {
            if (!request.CategoryId.HasValue)
            {
                return BadRequest("Income and Expense transactions require a CategoryId");
            }
        }
        
        // Validate account exists and belongs to user
        var account = await _accountRepository.GetByIdAndUserIdAsync(request.AccountId, userId);
        if (account == null)
        {
            return NotFound("Account not found");
        }
        
        // Create new transaction object for update
        var newTransaction = new Transaction
        {
            Id = oldTransaction.Id,
            Amount = request.Amount,
            Type = request.Type,
            Date = request.Date ?? oldTransaction.Date,
            Note = request.Note,
            AttachmentUrl = request.AttachmentUrl,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            ToAccountId = request.ToAccountId,
            UserId = userId,
            CreatedAt = oldTransaction.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
        
        try
        {
            await _transactionService.UpdateTransactionAsync(oldTransaction, newTransaction);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        
        // Update the transaction in repository
        oldTransaction.Amount = newTransaction.Amount;
        oldTransaction.Type = newTransaction.Type;
        oldTransaction.Date = newTransaction.Date;
        oldTransaction.Note = newTransaction.Note;
        oldTransaction.AttachmentUrl = newTransaction.AttachmentUrl;
        oldTransaction.AccountId = newTransaction.AccountId;
        oldTransaction.CategoryId = newTransaction.CategoryId;
        oldTransaction.ToAccountId = newTransaction.ToAccountId;
        oldTransaction.UpdatedAt = newTransaction.UpdatedAt;
        
        await _transactionRepository.UpdateAsync(oldTransaction);
        await _transactionRepository.SaveChangesAsync();
        
        // Reload with navigation properties
        oldTransaction = await _transactionRepository.GetByIdAndUserIdAsync(id, userId);
        
        var transactionDto = new TransactionDto
        {
            Id = oldTransaction.Id,
            Amount = oldTransaction.Amount,
            Type = oldTransaction.Type,
            Date = oldTransaction.Date,
            Note = oldTransaction.Note,
            AttachmentUrl = oldTransaction.AttachmentUrl,
            CreatedAt = oldTransaction.CreatedAt,
            UpdatedAt = oldTransaction.UpdatedAt,
            CategoryId = oldTransaction.CategoryId,
            CategoryName = oldTransaction.Category?.Name,
            AccountId = oldTransaction.AccountId,
            AccountName = oldTransaction.Account?.Name ?? string.Empty,
            ToAccountId = oldTransaction.ToAccountId,
            ToAccountName = oldTransaction.ToAccount?.Name
        };
        
        return Ok(transactionDto);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.GetCurrentUserId();
        var transaction = await _transactionRepository.GetByIdAndUserIdAsync(id, userId);
        
        if (transaction == null)
        {
            return NotFound();
        }
        
        try
        {
            await _transactionService.ReverseTransactionAsync(transaction);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        
        await _transactionRepository.DeleteAsync(transaction);
        await _transactionRepository.SaveChangesAsync();
        
        return NoContent();
    }
}