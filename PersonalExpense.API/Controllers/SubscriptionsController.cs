using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;

namespace PersonalExpense.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SubscriptionDto>>> GetSubscriptions(
        [FromQuery] SubscriptionStatus? status,
        [FromQuery] SubscriptionFrequency? frequency,
        [FromQuery] TransactionType? type,
        [FromQuery] DateTime? dueBefore,
        [FromQuery] DateTime? dueAfter,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        var filter = new SubscriptionFilterParams
        {
            Status = status,
            Frequency = frequency,
            Type = type,
            DueBefore = dueBefore,
            DueAfter = dueAfter,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _subscriptionService.GetSubscriptionsAsync(userId, filter);
        return Ok(result);
    }

    [HttpGet("reminders")]
    public async Task<ActionResult<List<SubscriptionReminderDto>>> GetReminders(
        [FromQuery] int daysInAdvance = 3)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        var reminders = await _subscriptionService.GetUpcomingRemindersAsync(userId, daysInAdvance);
        return Ok(reminders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(Guid id)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id, userId);

        if (subscription == null)
        {
            throw new NotFoundException(nameof(Subscription), id);
        }

        return Ok(subscription);
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionDto>> PostSubscription(SubscriptionCreateDto dto)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        var subscription = await _subscriptionService.CreateSubscriptionAsync(dto, userId);
        return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutSubscription(Guid id, SubscriptionUpdateDto dto)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        await _subscriptionService.UpdateSubscriptionAsync(id, dto, userId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubscription(Guid id)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        await _subscriptionService.DeleteSubscriptionAsync(id, userId);
        return NoContent();
    }

    [HttpPost("{id}/record-payment")]
    public async Task<ActionResult<TransactionDto>> RecordPayment(
        Guid id,
        RecordSubscriptionPaymentDto dto)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        var transaction = await _subscriptionService.RecordSubscriptionPaymentAsync(id, dto, userId);
        return Ok(transaction);
    }

    [HttpPost("generate-upcoming")]
    public async Task<ActionResult<List<TransactionDto>>> GenerateUpcomingTransactions()
    {
        var userId = this.GetCurrentUserIdOrThrow();
        var transactions = await _subscriptionService.GenerateUpcomingTransactionsAsync(userId);
        return Ok(transactions);
    }
}
