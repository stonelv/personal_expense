using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs;

public record TransactionCreateDto(
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string? Description,
    string? AttachmentUrl,
    Guid AccountId,
    Guid? CategoryId,
    Guid? TransferToAccountId
);

public record TransactionUpdateDto(
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
    Guid AccountId,
    string AccountName,
    Guid? CategoryId,
    string? CategoryName,
    Guid? TransferToAccountId,
    string? TransferToAccountName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record TransactionFilterDto(
    int? Year,
    int? Month,
    TransactionType? Type,
    int PageNumber = 1,
    int PageSize = 10
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
