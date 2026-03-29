using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _context;

    public BudgetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BudgetResponseDto>> GetBudgetsAsync(Guid userId, int? year, int? month)
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

        return await query
            .Select(b => new BudgetResponseDto(
                b.Id,
                b.Type,
                b.Amount,
                b.Year,
                b.Month,
                b.Description,
                b.CategoryId,
                b.Category!.Name,
                b.CreatedAt,
                b.UpdatedAt
            ))
            .ToListAsync();
    }

    public async Task<BudgetResponseDto?> GetBudgetByIdAsync(Guid id, Guid userId)
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
            budget.CategoryId,
            budget.Category!.Name,
            budget.CreatedAt,
            budget.UpdatedAt
        );
    }

    public async Task<BudgetStatusDto> GetBudgetStatusAsync(Guid userId, int year, int month)
    {
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

    public async Task<BudgetResponseDto> CreateBudgetAsync(BudgetCreateDto budgetDto, Guid userId)
    {
        if (budgetDto.Type == BudgetType.ByCategory && !budgetDto.CategoryId.HasValue)
        {
            throw new InvalidOperationException("CategoryId is required for category budgets");
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

        // Load category for response
        if (budget.CategoryId.HasValue)
        {
            await _context.Entry(budget)
                .Reference(b => b.Category)
                .LoadAsync();
        }

        return new BudgetResponseDto(
            budget.Id,
            budget.Type,
            budget.Amount,
            budget.Year,
            budget.Month,
            budget.Description,
            budget.CategoryId,
            budget.Category?.Name,
            budget.CreatedAt,
            budget.UpdatedAt
        );
    }

    public async Task<BudgetResponseDto> UpdateBudgetAsync(Guid id, BudgetUpdateDto budgetDto, Guid userId)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        if (budgetDto.Type == BudgetType.ByCategory && !budgetDto.CategoryId.HasValue)
        {
            throw new InvalidOperationException("CategoryId is required for category budgets");
        }

        budget.Type = budgetDto.Type;
        budget.Amount = budgetDto.Amount;
        budget.Year = budgetDto.Year;
        budget.Month = budgetDto.Month;
        budget.Description = budgetDto.Description;
        budget.UpdatedAt = DateTime.UtcNow;
        budget.CategoryId = budgetDto.CategoryId;

        await _context.SaveChangesAsync();

        // Load category for response
        if (budget.CategoryId.HasValue)
        {
            await _context.Entry(budget)
                .Reference(b => b.Category)
                .LoadAsync();
        }

        return new BudgetResponseDto(
            budget.Id,
            budget.Type,
            budget.Amount,
            budget.Year,
            budget.Month,
            budget.Description,
            budget.CategoryId,
            budget.Category?.Name,
            budget.CreatedAt,
            budget.UpdatedAt
        );
    }

    public async Task DeleteBudgetAsync(Guid id, Guid userId)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();
    }
}
