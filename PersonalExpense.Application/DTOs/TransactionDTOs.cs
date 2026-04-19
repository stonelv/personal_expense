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

public record TransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string? Description,
    string? AttachmentUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid AccountId,
    string? AccountName,
    Guid? CategoryId,
    string? CategoryName,
    Guid? TransferToAccountId,
    string? TransferToAccountName,
    Guid? SubscriptionId,
    bool IsGeneratedFromSubscription
);

public class TransactionFilterParams : PaginationParams
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public TransactionType? Type { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
