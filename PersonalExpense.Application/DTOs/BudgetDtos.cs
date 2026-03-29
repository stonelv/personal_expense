using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs;

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

public record BudgetResponseDto(
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
    bool IsOverBudget,
    List<CategorySpendingDto> CategorySpending
);

public record CategorySpendingDto(
    Guid? CategoryId,
    string CategoryName,
    decimal BudgetAmount,
    decimal SpentAmount,
    decimal Remaining,
    bool IsOverBudget
);
