using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs.Transaction;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Application.Exceptions;
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
    public async Task<ActionResult> GetTransactions(
        [FromQuery] TransactionQueryParameters parameters)
    {
        var userId = GetCurrentUserId();
        var result = await _transactionService.GetTransactionsAsync(userId, parameters);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);

        if (transaction == null)
        {
            throw new NotFoundException("Transaction", id);
        }

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> PostTransaction(TransactionCreateDto dto)
    {
        var userId = GetCurrentUserId();
        var transaction = await _transactionService.CreateTransactionAsync(userId, dto);
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTransaction(Guid id, TransactionUpdateDto dto)
    {
        var userId = GetCurrentUserId();
        await _transactionService.UpdateTransactionAsync(id, userId, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        await _transactionService.DeleteTransactionAsync(id, userId);
        return NoContent();
    }
}
