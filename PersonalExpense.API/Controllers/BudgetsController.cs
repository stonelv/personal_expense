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
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
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

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<BudgetDto>>> GetBudgets(
        [FromQuery] int? year, 
        [FromQuery] int? month)
    {
        var userId = GetCurrentUserId();
        var budgets = await _budgetService.GetBudgetsAsync(userId, year, month);
        return Ok(budgets);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetDto>> GetBudget(Guid id)
    {
        var userId = GetCurrentUserId();
        var budget = await _budgetService.GetBudgetByIdAsync(id, userId);
        
        if (budget == null)
        {
            throw new NotFoundException(nameof(Budget), id);
        }

        return Ok(budget);
    }

    [HttpGet("status")]
    public async Task<ActionResult<BudgetStatusDto>> GetBudgetStatus(
        [FromQuery] int year, 
        [FromQuery] int month,
        [FromQuery] string? timeZoneId = null)
    {
        var userId = GetCurrentUserId();
        var timeZone = GetTimeZoneFromRequest(timeZoneId);
        
        BudgetStatusDto status;
        if (timeZone != null)
        {
            status = await _budgetService.GetBudgetStatusAsync(userId, year, month, timeZone);
        }
        else
        {
            status = await _budgetService.GetBudgetStatusAsync(userId, year, month);
        }
        
        return Ok(status);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<BudgetAlertDto>> GetBudgetAlerts(
        [FromQuery] int year, 
        [FromQuery] int month,
        [FromQuery] string? timeZoneId = null)
    {
        var userId = GetCurrentUserId();
        var timeZone = GetTimeZoneFromRequest(timeZoneId);
        
        BudgetAlertDto alerts;
        if (timeZone != null)
        {
            alerts = await _budgetService.GetBudgetAlertsAsync(userId, year, month, timeZone);
        }
        else
        {
            alerts = await _budgetService.GetBudgetAlertsAsync(userId, year, month);
        }
        
        return Ok(alerts);
    }

    [HttpPost]
    public async Task<ActionResult<BudgetDto>> PostBudget(BudgetCreateDto dto)
    {
        var userId = GetCurrentUserId();
        var budget = await _budgetService.CreateBudgetAsync(dto, userId);
        return CreatedAtAction(nameof(GetBudget), new { id = budget.Id }, budget);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutBudget(Guid id, BudgetUpdateDto dto)
    {
        var userId = GetCurrentUserId();
        await _budgetService.UpdateBudgetAsync(id, dto, userId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var userId = GetCurrentUserId();
        await _budgetService.DeleteBudgetAsync(id, userId);
        return NoContent();
    }
}
