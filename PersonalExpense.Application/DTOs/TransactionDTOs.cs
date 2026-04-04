namespace PersonalExpense.Application.DTOs;

public record TransactionResponseDto(
    Guid Id,
    Domain.Entities.TransactionType Type,
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

public record CreateTransactionDto(
    Domain.Entities.TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string? Description,
    string? AttachmentUrl,
    Guid AccountId,
    Guid? CategoryId,
    Guid? TransferToAccountId
);

public record UpdateTransactionDto(
    Domain.Entities.TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string? Description,
    string? AttachmentUrl,
    Guid AccountId,
    Guid? CategoryId,
    Guid? TransferToAccountId
);

public record TransactionFilterDto(
    int? Year,
    int? Month,
    Domain.Entities.TransactionType? Type,
    int PageNumber = 1,
    int PageSize = 20
);

public record PagedResponseDto<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasNextPage,
    bool HasPreviousPage
);
