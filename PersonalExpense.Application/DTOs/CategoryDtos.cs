using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs;

public record CategoryCreateDto(
    string Name,
    CategoryType Type,
    string? Icon,
    string? Description
);

public record CategoryUpdateDto(
    string Name,
    CategoryType Type,
    string? Icon,
    string? Description
);

public record CategoryResponseDto(
    Guid Id,
    string Name,
    CategoryType Type,
    string? Icon,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
