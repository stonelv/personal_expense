using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs;

public enum PeriodType
{
    Weekly = 1,
    Monthly = 2
}

public record TrendDataPointDto(
    string Label,
    decimal TotalExpense,
    decimal TotalIncome,
    int TransactionCount
);

public record CategoryBreakdownDto(
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    decimal Percentage,
    int TransactionCount
);

public record MonthlyTrendDto(
    int Year,
    int Month,
    string MonthName,
    decimal TotalExpense,
    decimal TotalIncome,
    decimal NetAmount,
    List<CategoryBreakdownDto> CategoryBreakdown
);

public record MonthlyDetailDto(
    int Year,
    int Month,
    decimal TotalExpense,
    decimal TotalIncome,
    PagedResult<TransactionDto> Transactions
);

public record TrendAnalysisDto(
    PeriodType PeriodType,
    List<TrendDataPointDto> DataPoints,
    decimal AverageExpense,
    decimal HighestExpense,
    string HighestExpensePeriod,
    decimal LowestExpense,
    string LowestExpensePeriod
);

public class AnalyticsFilterParams
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
