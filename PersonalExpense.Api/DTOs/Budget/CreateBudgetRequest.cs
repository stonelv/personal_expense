using System.ComponentModel.DataAnnotations;

namespace PersonalExpense.Api.DTOs.Budget;

public class CreateBudgetRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [Required]
    [Range(1, 12)]
    public int Month { get; set; }
    
    [Required]
    [Range(2000, 2100)]
    public int Year { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    // Optional: If null, this is a total monthly budget
    public int? CategoryId { get; set; }
}