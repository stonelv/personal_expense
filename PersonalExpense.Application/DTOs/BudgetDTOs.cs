using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs;

public enum BudgetAlertLevel
{
    Normal = 0,
    Warning = 1,
    Critical = 2
}

public record BudgetCreateDto(
    BudgetType Type,
    decimal Amount,
    int Year,
    int Month,
    string? Description,
    Guid? CategoryId
);

public record BudgetUpdateDto(
    BudgetType Type,
    decimal Amount,
    int Year,
    int Month,
    string? Description,
    Guid? CategoryId
);

public record BudgetDto(
    Guid Id,
    BudgetType Type,
    decimal Amount,
    int Year,
    int Month,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record BudgetStatusDto(
    int Year,
    int Month,
    decimal TotalBudget,
    decimal TotalSpent,
    decimal Remaining,
    decimal Percentage,
    BudgetAlertLevel AlertLevel,
    bool IsOverBudget,
    List<CategorySpendingDto> CategorySpending
);

public record CategorySpendingDto(
    Guid? CategoryId,
    string CategoryName,
    decimal BudgetAmount,
    decimal SpentAmount,
    decimal Remaining,
    decimal Percentage,
    BudgetAlertLevel AlertLevel,
    bool IsOverBudget
);

public record BudgetAlertDto(
    int Year,
    int Month,
    BudgetAlertLevel OverallAlertLevel,
    string? OverallMessage,
    List<CategoryAlertDto> CategoryAlerts
);

public record CategoryAlertDto(
    Guid? CategoryId,
    string CategoryName,
    BudgetAlertLevel AlertLevel,
    string Message,
    decimal BudgetAmount,
    decimal SpentAmount,
    decimal Percentage
);

public record TransactionBudgetAlertDto(
    TransactionDto Transaction,
    BudgetAlertDto? BudgetAlert
);
