namespace PersonalExpense.Web.DTOs;

public enum TransactionType
{
    Income = 1,
    Expense = 2,
    Transfer = 3
}

public enum SubscriptionFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Yearly = 4
}

public enum SubscriptionStatus
{
    Active = 1,
    Paused = 2,
    Cancelled = 3
}

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

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}

public record AccountDto(
    Guid Id,
    string Name,
    int Type,
    decimal Balance,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CategoryDto(
    Guid Id,
    string Name,
    int Type,
    string? Icon,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
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
    string? TransferToAccountName
);
