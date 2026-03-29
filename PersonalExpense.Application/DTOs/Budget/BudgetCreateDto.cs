using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs.Budget;

public class BudgetCreateDto
{
    public BudgetType Type { get; set; }
    public decimal Amount { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
}
