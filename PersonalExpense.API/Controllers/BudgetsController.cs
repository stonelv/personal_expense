using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BudgetResponseDto>>> GetBudgets([FromQuery] int? year, [FromQuery] int? month)
    {
        var userId = GetCurrentUserId();
        var budgets = await _budgetService.GetBudgetsAsync(userId, year, month);
        return Ok(budgets);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetResponseDto>> GetBudget(Guid id)
    {
        var userId = GetCurrentUserId();
        var budget = await _budgetService.GetBudgetByIdAsync(id, userId);
        
        if (budget == null)
        {
            return NotFound();
        }

        return Ok(budget);
    }

    [HttpGet("status")]
    public async Task<ActionResult<BudgetStatusDto>> GetBudgetStatus([FromQuery] int year, [FromQuery] int month)
    {
        var userId = GetCurrentUserId();
        var status = await _budgetService.GetBudgetStatusAsync(userId, year, month);
        return Ok(status);
    }

    [HttpPost]
    public async Task<ActionResult<BudgetResponseDto>> PostBudget(BudgetCreateDto budgetDto)
    {
        var userId = GetCurrentUserId();
        var result = await _budgetService.CreateBudgetAsync(budgetDto, userId);
        return CreatedAtAction(nameof(GetBudget), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BudgetResponseDto>> PutBudget(Guid id, BudgetUpdateDto budgetDto)
    {
        var userId = GetCurrentUserId();
        var result = await _budgetService.UpdateBudgetAsync(id, budgetDto, userId);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var userId = GetCurrentUserId();
        await _budgetService.DeleteBudgetAsync(id, userId);
        return NoContent();
    }
}
