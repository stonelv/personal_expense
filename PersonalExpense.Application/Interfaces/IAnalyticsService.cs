using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface IAnalyticsService
{
    Task<TrendAnalysisDto> GetTrendAnalysisAsync(
        Guid userId,
        PeriodType periodType,
        AnalyticsFilterParams filter,
        int periodCount = 6);

    Task<List<MonthlyTrendDto>> GetLastSixMonthsTrendAsync(
        Guid userId,
        Guid? categoryId = null);

    Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync(
        Guid userId,
        int year,
        int month);

    Task<MonthlyDetailDto> GetMonthlyDetailAsync(
        Guid userId,
        int year,
        int month,
        Guid? categoryId = null,
        int pageNumber = 1,
        int pageSize = 20);
}
