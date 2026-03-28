using PersonalExpense.Domain.Enums;

namespace PersonalExpense.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    // For income/expense: this is the main account
    // For transfer: this is the source account
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    
    // For transfer only: this is the destination account
    public int? ToAccountId { get; set; }
    public Account? ToAccount { get; set; }
}