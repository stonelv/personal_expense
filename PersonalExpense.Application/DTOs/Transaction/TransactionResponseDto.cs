using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs.Transaction;

public class TransactionResponseDto
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid? TransferToAccountId { get; set; }
    public string? TransferToAccountName { get; set; }
}
