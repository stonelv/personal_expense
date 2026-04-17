using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Helpers;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class BudgetService : IBudgetService
{
    private const decimal WarningThreshold = 0.80m;
    private const decimal CriticalThreshold = 1.00m;

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
        return await GetBudgetStatusAsync(userId, year, month, TimeZoneInfo.Utc);
    }

    public async Task<BudgetStatusDto> GetBudgetStatusAsync(Guid userId, int year, int month, TimeZoneInfo timeZone)
    {
        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToListAsync();

        var monthStart = TimeZoneHelper.GetMonthStartInUtc(year, month, timeZone);
        var monthEnd = TimeZoneHelper.GetMonthEndInUtc(year, month, timeZone);

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId 
                && t.TransactionDate >= monthStart
                && t.TransactionDate <= monthEnd
                && t.Type == TransactionType.Expense)
            .ToListAsync();

        var totalBudget = budgets.Where(b => b.Type == BudgetType.Total).Sum(b => b.Amount);
        var categoryBudgets = budgets.Where(b => b.Type == BudgetType.ByCategory).ToList();
        var totalSpent = transactions.Sum(t => t.Amount);
        var totalPercentage = totalBudget > 0 ? totalSpent / totalBudget : 0;
        var totalAlertLevel = CalculateAlertLevel(totalPercentage);

        var categorySpending = categoryBudgets
            .Select(budget =>
            {
                var spent = transactions
                    .Where(t => t.CategoryId == budget.CategoryId)
                    .Sum(t => t.Amount);
                var percentage = budget.Amount > 0 ? spent / budget.Amount : 0;
                var alertLevel = CalculateAlertLevel(percentage);

                return new CategorySpendingDto(
                    budget.CategoryId,
                    budget.Category?.Name ?? "Unknown",
                    budget.Amount,
                    spent,
                    budget.Amount - spent,
                    percentage,
                    alertLevel,
                    spent >= budget.Amount
                );
            })
            .ToList();

        var uncategorizedSpent = transactions
            .Where(t => !t.CategoryId.HasValue || !categoryBudgets.Any(b => b.CategoryId == t.CategoryId))
            .Sum(t => t.Amount);

        if (uncategorizedSpent > 0)
        {
            categorySpending.Add(new CategorySpendingDto(
                null,
                "未分类",
                0,
                uncategorizedSpent,
                -uncategorizedSpent,
                0,
                BudgetAlertLevel.Normal,
                false
            ));
        }

        return new BudgetStatusDto(
            year,
            month,
            totalBudget,
            totalSpent,
            totalBudget - totalSpent,
            totalPercentage,
            totalAlertLevel,
            totalSpent >= totalBudget,
            categorySpending
        );
    }

    public async Task<BudgetAlertDto> GetBudgetAlertsAsync(Guid userId, int year, int month)
    {
        return await GetBudgetAlertsAsync(userId, year, month, TimeZoneInfo.Utc);
    }

    public async Task<BudgetAlertDto> GetBudgetAlertsAsync(Guid userId, int year, int month, TimeZoneInfo timeZone)
    {
        var status = await GetBudgetStatusAsync(userId, year, month, timeZone);

        var categoryAlerts = status.CategorySpending
            .Where(c => c.AlertLevel != BudgetAlertLevel.Normal && c.BudgetAmount > 0)
            .Select(c => new CategoryAlertDto(
                c.CategoryId,
                c.CategoryName,
                c.AlertLevel,
                GenerateAlertMessage(c.CategoryName, c.Percentage, c.AlertLevel),
                c.BudgetAmount,
                c.SpentAmount,
                c.Percentage
            ))
            .ToList();

        var overallAlertLevel = status.AlertLevel;
        string? overallMessage = null;

        if (status.TotalBudget > 0 && overallAlertLevel != BudgetAlertLevel.Normal)
        {
            overallMessage = GenerateAlertMessage("总预算", status.Percentage, overallAlertLevel);
        }

        var maxAlertLevel = categoryAlerts.Any() 
            ? categoryAlerts.Max(c => c.AlertLevel) 
            : BudgetAlertLevel.Normal;
        
        if (maxAlertLevel > overallAlertLevel)
        {
            overallAlertLevel = maxAlertLevel;
        }

        return new BudgetAlertDto(
            year,
            month,
            overallAlertLevel,
            overallMessage,
            categoryAlerts
        );
    }

    public async Task<BudgetAlertDto?> CheckBudgetAlertAfterTransactionAsync(
        Guid userId, 
        TransactionType transactionType, 
        DateTime transactionDate, 
        TimeZoneInfo? timeZone = null)
    {
        if (transactionType != TransactionType.Expense)
        {
            return null;
        }

        var tz = timeZone ?? TimeZoneInfo.Utc;
        var localDate = TimeZoneHelper.ConvertFromUtc(transactionDate, tz);
        var year = localDate.Year;
        var month = localDate.Month;

        var hasBudget = await _context.Budgets
            .AnyAsync(b => b.UserId == userId && b.Year == year && b.Month == month);

        if (!hasBudget)
        {
            return null;
        }

        return await GetBudgetAlertsAsync(userId, year, month, tz);
    }

    private static BudgetAlertLevel CalculateAlertLevel(decimal percentage)
    {
        if (percentage >= CriticalThreshold)
        {
            return BudgetAlertLevel.Critical;
        }
        if (percentage >= WarningThreshold)
        {
            return BudgetAlertLevel.Warning;
        }
        return BudgetAlertLevel.Normal;
    }

    private static string GenerateAlertMessage(string categoryName, decimal percentage, BudgetAlertLevel level)
    {
        var percentageDisplay = Math.Round(percentage * 100, 1);
        
        return level switch
        {
            BudgetAlertLevel.Warning => $"【提醒】{categoryName}已使用预算的 {percentageDisplay}%，即将达到预算上限",
            BudgetAlertLevel.Critical => $"【告警】{categoryName}已超支！当前使用 {percentageDisplay}%",
            _ => string.Empty
        };
    }



    public async Task<BudgetDto> CreateBudgetAsync(BudgetCreateDto dto, Guid userId)
    {
        if (dto.Type == BudgetType.ByCategory && !dto.CategoryId.HasValue)
        {
            throw new BadRequestException("CategoryId is required for category budgets");
        }

        var existingBudget = await _context.Budgets
            .FirstOrDefaultAsync(b => 
                b.UserId == userId && 
                b.Year == dto.Year && 
                b.Month == dto.Month &&
                b.Type == dto.Type &&
                b.CategoryId == dto.CategoryId);

        if (existingBudget != null)
        {
            throw new BadRequestException("该月份已存在相同类型的预算");
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

        var existingBudget = await _context.Budgets
            .FirstOrDefaultAsync(b => 
                b.Id != id &&
                b.UserId == userId && 
                b.Year == dto.Year && 
                b.Month == dto.Month &&
                b.Type == dto.Type &&
                b.CategoryId == dto.CategoryId);

        if (existingBudget != null)
        {
            throw new BadRequestException("该月份已存在相同类型的预算");
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
