using PersonalExpense.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PersonalExpense.Api.DTOs.Category;

public class CreateCategoryRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public CategoryType Type { get; set; }
    
    [StringLength(50)]
    public string? Icon { get; set; }
    
    [StringLength(20)]
    public string? Color { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
}