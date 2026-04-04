namespace PersonalExpense.Application.DTOs;

public record AccountResponseDto(
    Guid Id,
    string Name,
    Domain.Entities.AccountType Type,
    decimal Balance,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateAccountDto(
    string Name,
    Domain.Entities.AccountType Type,
    decimal Balance,
    string? Description
);

public record UpdateAccountDto(
    string Name,
    Domain.Entities.AccountType Type,
    decimal Balance,
    string? Description
);
