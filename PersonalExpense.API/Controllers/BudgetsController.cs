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
public class BudgetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BudgetsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Budget>>> GetBudgets([FromQuery] int? year, [FromQuery] int? month)
    {
        var userId = GetCurrentUserId();
        var query = _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        if (year.HasValue)
        {
            query = query.Where(b => b.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(b => b.Month == month.Value);
        }

        return await query.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Budget>> GetBudget(Guid id)
    {
        var userId = GetCurrentUserId();
        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        return budget;
    }

    [HttpGet("status")]
    public async Task<ActionResult<BudgetStatusDto>> GetBudgetStatus([FromQuery] int year, [FromQuery] int month)
    {
        var userId = GetCurrentUserId();

        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToListAsync();

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId 
                && t.TransactionDate.Year == year 
                && t.TransactionDate.Month == month 
                && t.Type == TransactionType.Expense)
            .ToListAsync();

        var totalBudget = budgets.Where(b => b.Type == BudgetType.Total).Sum(b => b.Amount);
        var categoryBudgets = budgets.Where(b => b.Type == BudgetType.ByCategory).ToList();
        
        var totalSpent = transactions.Sum(t => t.Amount);

        var categorySpending = transactions
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategorySpendingDto
            {
                CategoryId = g.Key,
                CategoryName = categoryBudgets.FirstOrDefault(b => b.CategoryId == g.Key)?.Category?.Name ?? "Unknown",
                BudgetAmount = categoryBudgets.FirstOrDefault(b => b.CategoryId == g.Key)?.Amount ?? 0,
                SpentAmount = g.Sum(t => t.Amount)
            })
            .ToList();

        return new BudgetStatusDto
        {
            Year = year,
            Month = month,
            TotalBudget = totalBudget,
            TotalSpent = totalSpent,
            Remaining = totalBudget - totalSpent,
            IsOverBudget = totalSpent > totalBudget,
            CategorySpending = categorySpending
        };
    }

    [HttpPost]
    public async Task<ActionResult<Budget>> PostBudget(BudgetDto budgetDto)
    {
        var userId = GetCurrentUserId();
        
        if (budgetDto.Type == BudgetType.ByCategory && !budgetDto.CategoryId.HasValue)
        {
            return BadRequest("CategoryId is required for category budgets");
        }

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            Type = budgetDto.Type,
            Amount = budgetDto.Amount,
            Year = budgetDto.Year,
            Month = budgetDto.Month,
            Description = budgetDto.Description,
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            CategoryId = budgetDto.CategoryId
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBudget), new { id = budget.Id }, budget);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutBudget(Guid id, BudgetDto budgetDto)
    {
        var userId = GetCurrentUserId();
        var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        if (budgetDto.Type == BudgetType.ByCategory && !budgetDto.CategoryId.HasValue)
        {
            return BadRequest("CategoryId is required for category budgets");
        }

        budget.Type = budgetDto.Type;
        budget.Amount = budgetDto.Amount;
        budget.Year = budgetDto.Year;
        budget.Month = budgetDto.Month;
        budget.Description = budgetDto.Description;
        budget.UpdatedAt = DateTime.UtcNow;
        budget.CategoryId = budgetDto.CategoryId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var userId = GetCurrentUserId();
        var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record BudgetDto(BudgetType Type, decimal Amount, int Year, int Month, string? Description, Guid? CategoryId);
public record BudgetStatusDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining { get; set; }
    public bool IsOverBudget { get; set; }
    public List<CategorySpendingDto> CategorySpending { get; set; } = new();
}

public record CategorySpendingDto
{
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal Remaining => BudgetAmount - SpentAmount;
    public bool IsOverBudget => SpentAmount > BudgetAmount;
}
