using PersonalExpense.Application.DTOs.Category;

namespace PersonalExpense.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponseDto>> GetCategoriesAsync(Guid userId);
    Task<CategoryResponseDto?> GetCategoryByIdAsync(Guid id, Guid userId);
    Task<CategoryResponseDto> CreateCategoryAsync(Guid userId, CategoryCreateDto dto);
    Task UpdateCategoryAsync(Guid id, Guid userId, CategoryUpdateDto dto);
    Task DeleteCategoryAsync(Guid id, Guid userId);
}
