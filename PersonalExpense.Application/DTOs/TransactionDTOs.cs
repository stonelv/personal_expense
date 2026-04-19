using PersonalExpense.Domain.Entities;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

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
    string? TransferToAccountName
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

public record CsvTransactionRecord
{
    [Name("日期")]
    public string? Date { get; set; }

    [Name("分类")]
    public string? Category { get; set; }

    [Name("金额")]
    public string? Amount { get; set; }

    [Name("备注")]
    public string? Description { get; set; }
}

public record ImportErrorDto
{
    public int RowNumber { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? RawData { get; set; }
}

public record ImportResultDto
{
    public int TotalRows { get; set; }
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = new();
    public bool IsSuccess => ErrorCount == 0;
}

public record TransactionImportDto
{
    public DateTime TransactionDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public int RowNumber { get; set; }
}
