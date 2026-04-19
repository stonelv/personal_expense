using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
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
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetTransactions(
        [FromQuery] int? year, 
        [FromQuery] int? month,
        [FromQuery] TransactionType? type,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var filter = new TransactionFilterParams
        {
            Year = year,
            Month = month,
            Type = type,
            AccountId = accountId,
            CategoryId = categoryId,
            StartDate = startDate,
            EndDate = endDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _transactionService.GetTransactionsAsync(userId, filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);
        
        if (transaction == null)
        {
            throw new NotFoundException(nameof(Transaction), id);
        }

        return Ok(transaction);
    }

    [HttpGet("account/{accountId}/history")]
    public async Task<ActionResult<AccountBalanceHistoryDto>> GetAccountBalanceHistory(
        [FromRoute] Guid accountId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = GetCurrentUserId();
        var result = await _transactionService.GetAccountBalanceHistoryAsync(accountId, userId, startDate, endDate);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> PostTransaction(TransactionCreateDto dto)
    {
        var userId = GetCurrentUserId();
        var transaction = await _transactionService.CreateTransactionAsync(dto, userId);
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<TransferResultDto>> PostTransfer(TransferCreateDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _transactionService.CreateTransferAsync(dto, userId);
        return CreatedAtAction(nameof(GetTransaction), new { id = result.OutgoingTransaction.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTransaction(Guid id, TransactionUpdateDto dto)
    {
        var userId = GetCurrentUserId();
        await _transactionService.UpdateTransactionAsync(id, dto, userId);
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
