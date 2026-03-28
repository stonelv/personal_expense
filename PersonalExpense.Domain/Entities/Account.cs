using PersonalExpense.Domain.Enums;

namespace PersonalExpense.Domain.Entities;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string? Description { get; set; }
    public string? Currency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Navigation properties
    public ICollection<Transaction> FromTransactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> ToTransactions { get; set; } = new List<Transaction>();
}