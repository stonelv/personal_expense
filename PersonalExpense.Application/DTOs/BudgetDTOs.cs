namespace PersonalExpense.Application.DTOs;

public record BudgetResponseDto(
    Guid Id,
    Domain.Entities.BudgetType Type,
    decimal Amount,
    int Year,
    int Month,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? CategoryId,
    string? CategoryName
);

public record CreateBudgetDto(
    Domain.Entities.BudgetType Type,
    decimal Amount,
    int Year,
    int Month,
    string? Description,
    Guid? CategoryId
);

public record UpdateBudgetDto(
    Domain.Entities.BudgetType Type,
    decimal Amount,
    int Year,
    int Month,
    string? Description,
    Guid? CategoryId
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
