using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Helpers;
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

    private static TimeZoneInfo? GetTimeZoneFromRequest(string? timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId))
        {
            return null;
        }

        return TimeZoneHelper.FindTimeZoneById(timeZoneId);
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

    [HttpPost]
    public async Task<ActionResult<TransactionBudgetAlertDto>> PostTransaction(
        TransactionCreateDto dto,
        [FromQuery] string? timeZoneId = null)
    {
        var userId = GetCurrentUserId();
        var timeZone = GetTimeZoneFromRequest(timeZoneId);
        var result = await _transactionService.CreateTransactionAsync(dto, userId, timeZone);
        return CreatedAtAction(nameof(GetTransaction), new { id = result.Transaction.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionBudgetAlertDto>> PutTransaction(
        Guid id, 
        TransactionUpdateDto dto,
        [FromQuery] string? timeZoneId = null)
    {
        var userId = GetCurrentUserId();
        var timeZone = GetTimeZoneFromRequest(timeZoneId);
        var result = await _transactionService.UpdateTransactionAsync(id, dto, userId, timeZone);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<BudgetAlertDto?>> DeleteTransaction(
        Guid id,
        [FromQuery] string? timeZoneId = null)
    {
        var userId = GetCurrentUserId();
        var timeZone = GetTimeZoneFromRequest(timeZoneId);
        var result = await _transactionService.DeleteTransactionAsync(id, userId, timeZone);
        return Ok(result);
    }
}
