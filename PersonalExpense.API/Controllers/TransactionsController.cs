using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using System.Security.Claims;

namespace PersonalExpense.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionResponseDto>>> GetTransactions(
        [FromQuery] int? year, 
        [FromQuery] int? month,
        [FromQuery] TransactionType? type,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        var filter = new TransactionFilterDto(year, month, type, pageNumber, pageSize);
        var result = await _transactionService.GetTransactionsAsync(userId, filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);
        
        if (transaction == null)
        {
            return NotFound();
        }

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> PostTransaction(TransactionCreateDto transactionDto)
    {
        var userId = GetCurrentUserId();
        var result = await _transactionService.CreateTransactionAsync(transactionDto, userId);
        return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> PutTransaction(Guid id, TransactionUpdateDto transactionDto)
    {
        var userId = GetCurrentUserId();
        var result = await _transactionService.UpdateTransactionAsync(id, transactionDto, userId);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        await _transactionService.DeleteTransactionAsync(id, userId);
        return NoContent();
    }
}
