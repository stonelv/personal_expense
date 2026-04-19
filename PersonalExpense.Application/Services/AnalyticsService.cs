using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;
using System.Globalization;

namespace PersonalExpense.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrendAnalysisDto> GetTrendAnalysisAsync(
        Guid userId,
        PeriodType periodType,
        AnalyticsFilterParams filter,
        int periodCount = 6)
    {
        var now = DateTime.UtcNow;
        var dataPoints = new List<TrendDataPointDto>();

        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Type != TransactionType.Transfer);

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= filter.EndDate.Value);
        }

        if (periodType == PeriodType.Monthly)
        {
            for (int i = 0; i < periodCount; i++)
            {
                var targetDate = now.AddMonths(-i);
                var year = targetDate.Year;
                var month = targetDate.Month;

                var monthlyQuery = query
                    .Where(t => t.TransactionDate.Year == year && t.TransactionDate.Month == month);

                var dataPoint = await CalculateDataPointAsync(
                    monthlyQuery,
                    $"{year}年{month}月"
                );

                dataPoints.Insert(0, dataPoint);
            }
        }
        else
        {
            for (int i = 0; i < periodCount; i++)
            {
                var weekStart = now.AddDays(-(i * 7 + (int)now.DayOfWeek));
                var weekEnd = weekStart.AddDays(7).AddTicks(-1);

                var weeklyQuery = query
                    .Where(t => t.TransactionDate >= weekStart && t.TransactionDate <= weekEnd);

                var dataPoint = await CalculateDataPointAsync(
                    weeklyQuery,
                    $"{weekStart:MM/dd}-{weekEnd:MM/dd}"
                );

                dataPoints.Insert(0, dataPoint);
            }
        }

        var expensePoints = dataPoints.Where(p => p.TotalExpense > 0).ToList();
        var averageExpense = expensePoints.Any() ? expensePoints.Average(p => p.TotalExpense) : 0;
        var highestExpense = expensePoints.Any() ? expensePoints.Max(p => p.TotalExpense) : 0;
        var lowestExpense = expensePoints.Any() ? expensePoints.Min(p => p.TotalExpense) : 0;

        var highestExpensePoint = expensePoints.FirstOrDefault(p => p.TotalExpense == highestExpense);
        var lowestExpensePoint = expensePoints.FirstOrDefault(p => p.TotalExpense == lowestExpense);

        return new TrendAnalysisDto(
            PeriodType: periodType,
            DataPoints: dataPoints,
            AverageExpense: Math.Round(averageExpense, 2),
            HighestExpense: Math.Round(highestExpense, 2),
            HighestExpensePeriod: highestExpensePoint?.Label ?? string.Empty,
            LowestExpense: Math.Round(lowestExpense, 2),
            LowestExpensePeriod: lowestExpensePoint?.Label ?? string.Empty
        );
    }

    public async Task<List<MonthlyTrendDto>> GetLastSixMonthsTrendAsync(
        Guid userId,
        Guid? categoryId = null)
    {
        var now = DateTime.UtcNow;
        var result = new List<MonthlyTrendDto>();

        for (int i = 0; i < 6; i++)
        {
            var targetDate = now.AddMonths(-i);
            var year = targetDate.Year;
            var month = targetDate.Month;

            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId
                    && t.TransactionDate.Year == year
                    && t.TransactionDate.Month == month
                    && t.Type != TransactionType.Transfer);

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            var totalExpense = await query
                .Where(t => t.Type == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalIncome = await query
                .Where(t => t.Type == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var categoryBreakdown = await GetCategoryBreakdownFromQueryAsync(
                query.Where(t => t.Type == TransactionType.Expense)
            );

            var monthName = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetMonthName(month);

            result.Insert(0, new MonthlyTrendDto(
                Year: year,
                Month: month,
                MonthName: monthName,
                TotalExpense: Math.Round(totalExpense, 2),
                TotalIncome: Math.Round(totalIncome, 2),
                NetAmount: Math.Round(totalIncome - totalExpense, 2),
                CategoryBreakdown: categoryBreakdown
            ));
        }

        return result;
    }

    public async Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync(
        Guid userId,
        int year,
        int month)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId
                && t.TransactionDate.Year == year
                && t.TransactionDate.Month == month
                && t.Type == TransactionType.Expense);

        return await GetCategoryBreakdownFromQueryAsync(query);
    }

    public async Task<MonthlyDetailDto> GetMonthlyDetailAsync(
        Guid userId,
        int year,
        int month,
        Guid? categoryId = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .Where(t => t.UserId == userId
                && t.TransactionDate.Year == year
                && t.TransactionDate.Month == month
                && t.Type != TransactionType.Transfer);

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        var totalExpense = await query
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var totalIncome = await query
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new MonthlyDetailDto(
            Year: year,
            Month: month,
            TotalExpense: Math.Round(totalExpense, 2),
            TotalIncome: Math.Round(totalIncome, 2),
            Transactions: new PagedResult<TransactionDto>
            {
                Items = items.Select(MapToTransactionDto).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            }
        );
    }

    private async Task<TrendDataPointDto> CalculateDataPointAsync(
        IQueryable<Transaction> query,
        string label)
    {
        var totalExpense = await query
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var totalIncome = await query
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var count = await query.CountAsync();

        return new TrendDataPointDto(
            Label: label,
            TotalExpense: Math.Round(totalExpense, 2),
            TotalIncome: Math.Round(totalIncome, 2),
            TransactionCount: count
        );
    }

    private async Task<List<CategoryBreakdownDto>> GetCategoryBreakdownFromQueryAsync(
        IQueryable<Transaction> query)
    {
        var categoryGroups = await query
            .Where(t => t.CategoryId != null)
            .GroupBy(t => new { t.CategoryId, t.Category!.Name })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId!.Value,
                CategoryName = g.Key.Name,
                Amount = (decimal?)g.Sum(t => t.Amount) ?? 0,
                Count = g.Count()
            })
            .ToListAsync();

        var totalAmount = categoryGroups.Sum(g => g.Amount);

        return categoryGroups
            .OrderByDescending(g => g.Amount)
            .Select(g => new CategoryBreakdownDto(
                CategoryId: g.CategoryId,
                CategoryName: g.CategoryName,
                Amount: Math.Round(g.Amount, 2),
                Percentage: totalAmount > 0 ? Math.Round((g.Amount / totalAmount) * 100, 2) : 0,
                TransactionCount: g.Count
            ))
            .ToList();
    }

    private static TransactionDto MapToTransactionDto(Transaction transaction)
    {
        return new TransactionDto(
            transaction.Id,
            transaction.Type,
            transaction.Amount,
            transaction.TransactionDate,
            transaction.Description,
            transaction.AttachmentUrl,
            transaction.CreatedAt,
            transaction.UpdatedAt,
            transaction.AccountId,
            transaction.Account?.Name,
            transaction.CategoryId,
            transaction.Category?.Name,
            transaction.TransferToAccountId,
            transaction.TransferToAccount?.Name
        );
    }
}
