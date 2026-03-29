using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs.Category;

public class CategoryUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string? Icon { get; set; }
    public string? Description { get; set; }
}
