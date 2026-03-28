namespace PersonalExpense.Domain.Entities;

public class Budget
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Optional: If null, this is a total budget for the month
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
}