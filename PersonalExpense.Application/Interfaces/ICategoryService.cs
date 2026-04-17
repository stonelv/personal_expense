using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(Guid userId);
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id, Guid userId);
    Task<CategoryDto> CreateCategoryAsync(CategoryCreateDto dto, Guid userId);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, CategoryUpdateDto dto, Guid userId);
    Task DeleteCategoryAsync(Guid id, Guid userId);
}
