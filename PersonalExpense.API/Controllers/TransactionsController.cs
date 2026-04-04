using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using System.Security.Claims;

namespace PersonalExpense.Api.Controllers;

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
    public async Task<ActionResult<PagedResponseDto<TransactionResponseDto>>> GetTransactions(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] PersonalExpense.Domain.Entities.TransactionType? type,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var filter = new TransactionFilterDto(year, month, type, pageNumber, pageSize);
        var result = await _transactionService.GetTransactionsAsync(filter, userId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        var transaction = await _transactionService.GetTransactionAsync(id, userId);

        if (transaction == null)
        {
            return NotFound();
        }

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> PostTransaction(CreateTransactionDto dto)
    {
        var userId = GetCurrentUserId();
        var transaction = await _transactionService.CreateTransactionAsync(dto, userId);
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTransaction(Guid id, UpdateTransactionDto dto)
    {
        var userId = GetCurrentUserId();
        var success = await _transactionService.UpdateTransactionAsync(id, dto, userId);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        var success = await _transactionService.DeleteTransactionAsync(id, userId);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
