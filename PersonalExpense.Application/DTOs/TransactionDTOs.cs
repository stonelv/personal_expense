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

public record TransferCreateDto(
    decimal Amount,
    DateTime TransactionDate,
    string? Description,
    string? AttachmentUrl,
    Guid FromAccountId,
    Guid ToAccountId
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
    Guid? RelatedTransactionId
);

public record TransferResultDto(
    TransactionDto OutgoingTransaction,
    TransactionDto IncomingTransaction
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

public record AccountBalanceHistoryDto(
    Guid AccountId,
    string AccountName,
    AccountType AccountType,
    List<BalanceEntryDto> BalanceHistory,
    decimal StartingBalance,
    decimal EndingBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetChange
);

public record BalanceEntryDto(
    DateTime TransactionDate,
    string? Description,
    TransactionType TransactionType,
    decimal Amount,
    decimal BalanceAfterTransaction,
    Guid? RelatedTransactionId
);

public record ReconciliationResultDto(
    Guid AccountId,
    string AccountName,
    AccountType AccountType,
    bool IsBalanced,
    decimal ExpectedBalance,
    decimal ActualBalance,
    decimal Discrepancy,
    List<DiscrepancyItemDto> Discrepancies,
    DateTime ReconciliationDate
);

public record DiscrepancyItemDto(
    string Type,
    string Description,
    decimal Expected,
    decimal Actual,
    decimal Difference
);
