namespace PersonalExpense.Application.DTOs.Budget;

public class CategorySpendingDto
{
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal Remaining => BudgetAmount - SpentAmount;
    public bool IsOverBudget => SpentAmount > BudgetAmount;
}
