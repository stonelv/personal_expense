using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponseDto>> GetCategoriesAsync(Guid userId);
    Task<CategoryResponseDto?> GetCategoryAsync(Guid id, Guid userId);
    Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto dto, Guid userId);
    Task<bool> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto, Guid userId);
    Task<bool> DeleteCategoryAsync(Guid id, Guid userId);
}
