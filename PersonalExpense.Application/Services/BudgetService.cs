using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs.Budget;
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
            .Select(b => new BudgetResponseDto
            {
                Id = b.Id,
                Type = b.Type,
                Amount = b.Amount,
                Year = b.Year,
                Month = b.Month,
                Description = b.Description,
                CategoryId = b.CategoryId,
                CategoryName = b.Category != null ? b.Category.Name : null,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<BudgetResponseDto?> GetBudgetByIdAsync(Guid id, Guid userId)
    {
        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            return null;
        }

        return new BudgetResponseDto
        {
            Id = budget.Id,
            Type = budget.Type,
            Amount = budget.Amount,
            Year = budget.Year,
            Month = budget.Month,
            Description = budget.Description,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category != null ? budget.Category.Name : null,
            CreatedAt = budget.CreatedAt,
            UpdatedAt = budget.UpdatedAt
        };
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

    public async Task<BudgetResponseDto> CreateBudgetAsync(Guid userId, BudgetCreateDto dto)
    {
        if (dto.Type == BudgetType.ByCategory && !dto.CategoryId.HasValue)
        {
            throw new BadRequestException("CategoryId is required for category budgets");
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

        return await GetBudgetByIdAsync(budget.Id, userId) ?? throw new NotFoundException("Budget not found after creation");
    }

    public async Task UpdateBudgetAsync(Guid id, Guid userId, BudgetUpdateDto dto)
    {
        var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            throw new NotFoundException("Budget", id);
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
    }

    public async Task DeleteBudgetAsync(Guid id, Guid userId)
    {
        var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
        {
            throw new NotFoundException("Budget", id);
        }

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();
    }
}
