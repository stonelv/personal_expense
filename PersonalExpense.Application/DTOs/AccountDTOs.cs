using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs;

public record AccountCreateDto(
    string Name,
    AccountType Type,
    decimal Balance,
    string? Description
);

public record AccountUpdateDto(
    string Name,
    AccountType Type,
    decimal Balance,
    string? Description
);

public record AccountDto(
    Guid Id,
    string Name,
    AccountType Type,
    decimal Balance,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
