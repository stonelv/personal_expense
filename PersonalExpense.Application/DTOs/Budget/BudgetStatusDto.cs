namespace PersonalExpense.Application.DTOs.Budget;

public class BudgetStatusDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining { get; set; }
    public bool IsOverBudget { get; set; }
    public List<CategorySpendingDto> CategorySpending { get; set; } = new();
}
