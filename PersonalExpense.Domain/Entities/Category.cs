using PersonalExpense.Domain.Enums;

namespace PersonalExpense.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}