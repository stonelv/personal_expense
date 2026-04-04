namespace PersonalExpense.Application.DTOs;

public record CategoryResponseDto(
    Guid Id,
    string Name,
    Domain.Entities.CategoryType Type,
    string? Icon,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCategoryDto(
    string Name,
    Domain.Entities.CategoryType Type,
    string? Icon,
    string? Description
);

public record UpdateCategoryDto(
    string Name,
    Domain.Entities.CategoryType Type,
    string? Icon,
    string? Description
);
