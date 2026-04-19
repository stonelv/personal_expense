using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs;

public record SubscriptionCreateDto(
    string Name,
    decimal Amount,
    TransactionType Type,
    SubscriptionFrequency Frequency,
    DateTime StartDate,
    DateTime? EndDate,
    string? Description,
    Guid AccountId,
    Guid? CategoryId
);

public record SubscriptionUpdateDto(
    string Name,
    decimal Amount,
    TransactionType Type,
    SubscriptionFrequency Frequency,
    DateTime StartDate,
    DateTime? EndDate,
    SubscriptionStatus Status,
    string? Description,
    Guid AccountId,
    Guid? CategoryId
);

public record SubscriptionDto(
    Guid Id,
    string Name,
    decimal Amount,
    TransactionType Type,
    SubscriptionFrequency Frequency,
    DateTime StartDate,
    DateTime? EndDate,
    DateTime NextDueDate,
    DateTime? LastPaidDate,
    SubscriptionStatus Status,
    string? Description,
    Guid AccountId,
    string? AccountName,
    Guid? CategoryId,
    string? CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int DaysUntilDue
);

public record SubscriptionFilterParams : PaginationParams
{
    public SubscriptionStatus? Status { get; set; }
    public SubscriptionFrequency? Frequency { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? DueBefore { get; set; }
    public DateTime? DueAfter { get; set; }
}

public record SubscriptionReminderDto(
    Guid SubscriptionId,
    string SubscriptionName,
    decimal Amount,
    TransactionType Type,
    DateTime NextDueDate,
    int DaysUntilDue,
    Guid AccountId,
    string? AccountName
);

public record RecordSubscriptionPaymentDto(
    DateTime PaymentDate,
    string? Description,
    string? AttachmentUrl
);
