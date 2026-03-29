using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs.Transaction;

public class TransactionCreateDto
{
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }
    public Guid AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? TransferToAccountId { get; set; }
}
