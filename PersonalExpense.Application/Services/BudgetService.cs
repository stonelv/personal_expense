using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
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

    public async Task<List<BudgetDto>> GetBudgetsAsync(Guid userId, int? year, int? month)
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
        return budgets.Select(MapToDto).ToList();
    }

    public async Task<BudgetDto?> GetBudgetByIdAsync(Guid id, Guid userId)
    {
        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        return budget != null ? MapToDto(budget) : null;
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
            .Select(g =>
            {
                var budget = categoryBudgets.FirstOrDefault(b => b.CategoryId == g.Key);
                var spent = g.Sum(t => t.Amount);
                var budgetAmount = budget?.Amount ?? 0;
                return new CategorySpendingDto(
                    g.Key,
                    budget?.Category?.Name ?? "Unknown",
                    budgetAmount,
                    spent,
                    budgetAmount - spent,
                    spent > budgetAmount
                );
            })
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

    public async Task<BudgetDto> CreateBudgetAsync(BudgetCreateDto dto, Guid userId)
    {
        if (dto.Type == BudgetType.ByCategory && !dto.CategoryId.HasValue)
        {
            throw new BadRequestException("CategoryId is required for category budgets");
        }

        var budget = new Budget
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

        return MapToDto(budget);
    }

    public async Task<BudgetDto> UpdateBudgetAsync(Guid id, BudgetUpdateDto dto, Guid userId)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            throw new NotFoundException(nameof(Budget), id);
        }

        if (dto.Type == BudgetType.ByCategory && !dto.CategoryId.HasValue)
        {
            throw new BadRequestException("CategoryId is required for category budgets");
        }

        budget.Type = dto.Type;
        budget.Amount = dto.Amount;
        budget.Year = dto.Year;
        budget.Month = dto.Month;
        budget.Description = dto.Description;
        budget.UpdatedAt = DateTime.UtcNow;
        budget.CategoryId = dto.CategoryId;

        await _context.SaveChangesAsync();

        return MapToDto(budget);
    }

    public async Task DeleteBudgetAsync(Guid id, Guid userId)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            throw new NotFoundException(nameof(Budget), id);
        }

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();
    }

    private static BudgetDto MapToDto(Budget budget)
    {
        return new BudgetDto(
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
}
