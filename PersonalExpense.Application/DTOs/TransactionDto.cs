using PersonalExpense.Domain.Entities;
using System;

namespace PersonalExpense.Application.DTOs;

public record TransactionRequestDto(
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string? Description,
    string? AttachmentUrl,
    Guid AccountId,
    Guid? CategoryId,
    Guid? TransferToAccountId
);

public record TransactionResponseDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string? Description,
    string? AttachmentUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid AccountId,
    string AccountName,
    Guid? CategoryId,
    string? CategoryName,
    Guid? TransferToAccountId,
    string? TransferToAccountName
);

public record TransactionListRequestDto(
    int? Year,
    int? Month,
    TransactionType? Type,
    int Page = 1,
    int PageSize = 10
);

public record TransactionListResponseDto(
    IEnumerable<TransactionResponseDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
