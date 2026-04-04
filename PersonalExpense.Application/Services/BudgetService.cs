using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _context;

    public BudgetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BudgetResponseDto>> GetBudgetsAsync(Guid userId, int? year = null, int? month = null)
    {
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

        var budgets = await query.ToListAsync();

        return budgets.Select(b => new BudgetResponseDto(
            b.Id,
            b.Type,
            b.Amount,
            b.Year,
            b.Month,
            b.Description,
            b.CreatedAt,
            b.UpdatedAt,
            b.CategoryId,
            b.Category?.Name
        ));
    }

    public async Task<BudgetResponseDto?> GetBudgetAsync(Guid id, Guid userId)
    {
        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return null;

        return new BudgetResponseDto(
            budget.Id,
            budget.Type,
            budget.Amount,
            budget.Year,
            budget.Month,
            budget.Description,
            budget.CreatedAt,
            budget.UpdatedAt,
            budget.CategoryId,
            budget.Category?.Name
        );
    }

    public async Task<BudgetStatusDto> GetBudgetStatusAsync(int year, int month, Guid userId)
    {
        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToListAsync();

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId
                && t.TransactionDate.Year == year
                && t.TransactionDate.Month == month
                && t.Type == Domain.Entities.TransactionType.Expense)
            .ToListAsync();

        var totalBudget = budgets.Where(b => b.Type == Domain.Entities.BudgetType.Total).Sum(b => b.Amount);
        var categoryBudgets = budgets.Where(b => b.Type == Domain.Entities.BudgetType.ByCategory).ToList();

        var totalSpent = transactions.Sum(t => t.Amount);

        var categorySpending = transactions
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategorySpendingDto(
                g.Key,
                categoryBudgets.FirstOrDefault(b => b.CategoryId == g.Key)?.Category?.Name ?? "Unknown",
                categoryBudgets.FirstOrDefault(b => b.CategoryId == g.Key)?.Amount ?? 0,
                g.Sum(t => t.Amount),
                (categoryBudgets.FirstOrDefault(b => b.CategoryId == g.Key)?.Amount ?? 0) - g.Sum(t => t.Amount),
                g.Sum(t => t.Amount) > (categoryBudgets.FirstOrDefault(b => b.CategoryId == g.Key)?.Amount ?? 0)
            ))
            .ToList();

        return new BudgetStatusDto(
            year,
            month,
            totalBudget,
            totalSpent,
            totalBudget - totalSpent,
            totalSpent > totalBudget,
            categorySpending
        );
    }

    public async Task<BudgetResponseDto> CreateBudgetAsync(CreateBudgetDto dto, Guid userId)
    {
        if (dto.Type == Domain.Entities.BudgetType.ByCategory && !dto.CategoryId.HasValue)
        {
            throw new ArgumentException("CategoryId is required for category budgets");
        }

        var budget = new Domain.Entities.Budget
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            Amount = dto.Amount,
            Year = dto.Year,
            Month = dto.Month,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            CategoryId = dto.CategoryId
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        return await GetBudgetAsync(budget.Id, userId) ?? throw new InvalidOperationException("Budget not found after creation");
    }

    public async Task<bool> UpdateBudgetAsync(Guid id, UpdateBudgetDto dto, Guid userId)
    {
        var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return false;

        if (dto.Type == Domain.Entities.BudgetType.ByCategory && !dto.CategoryId.HasValue)
        {
            throw new ArgumentException("CategoryId is required for category budgets");
        }

        budget.Type = dto.Type;
        budget.Amount = dto.Amount;
        budget.Year = dto.Year;
        budget.Month = dto.Month;
        budget.Description = dto.Description;
        budget.UpdatedAt = DateTime.UtcNow;
        budget.CategoryId = dto.CategoryId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteBudgetAsync(Guid id, Guid userId)
    {
        var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return false;

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();
        return true;
    }
}
