using PersonalExpense.Domain.Enums;

namespace PersonalExpense.Api.DTOs.Transaction;

public class TransactionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    
    public int? ToAccountId { get; set; }
    public string? ToAccountName { get; set; }
}