namespace PersonalExpense.Domain.Entities;

public enum BudgetType
{
    Total = 1,
    ByCategory = 2
}

public class Budget
{
    public Guid Id { get; set; }
    public BudgetType Type { get; set; }
    public decimal Amount { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
}
