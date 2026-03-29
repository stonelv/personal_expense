using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponseDto>> GetCategoriesAsync(Guid userId);
    Task<CategoryResponseDto?> GetCategoryByIdAsync(Guid id, Guid userId);
    Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto categoryDto, Guid userId);
    Task<CategoryResponseDto> UpdateCategoryAsync(Guid id, CategoryUpdateDto categoryDto, Guid userId);
    Task DeleteCategoryAsync(Guid id, Guid userId);
}
