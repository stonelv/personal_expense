namespace PersonalExpense.Api.DTOs.Budget;

public class BudgetDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    
    // Calculated fields
    public decimal Spent { get; set; }
    public decimal Remaining => Amount - Spent;
    public bool IsOverBudget => Spent > Amount;
    public decimal PercentageSpent => Amount > 0 ? Math.Round((Spent / Amount) * 100, 2) : 0;
}