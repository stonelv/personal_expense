using PersonalExpense.Domain.Entities;
using System;

namespace PersonalExpense.Application.DTOs;

public record AccountRequestDto(
    string Name,
    AccountType Type,
    decimal Balance,
    string? Description
);

public record AccountResponseDto(
    Guid Id,
    string Name,
    AccountType Type,
    decimal Balance,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
