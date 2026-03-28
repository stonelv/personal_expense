using System.ComponentModel.DataAnnotations;

namespace PersonalExpense.Api.DTOs.Budget;

public class UpdateBudgetRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
}