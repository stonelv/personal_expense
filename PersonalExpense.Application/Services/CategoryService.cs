using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryResponseDto>> GetCategoriesAsync(Guid userId)
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .ToListAsync();

        return categories.Select(c => new CategoryResponseDto(
            c.Id,
            c.Name,
            c.Type,
            c.Icon,
            c.Description,
            c.IsActive,
            c.CreatedAt,
            c.UpdatedAt
        ));
    }

    public async Task<CategoryResponseDto?> GetCategoryAsync(Guid id, Guid userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return null;

        return new CategoryResponseDto(
            category.Id,
            category.Name,
            category.Type,
            category.Icon,
            category.Description,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt
        );
    }

    public async Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto dto, Guid userId)
    {
        var category = new Domain.Entities.Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Type = dto.Type,
            Icon = dto.Icon,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return new CategoryResponseDto(
            category.Id,
            category.Name,
            category.Type,
            category.Icon,
            category.Description,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt
        );
    }

    public async Task<bool> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto, Guid userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return false;

        category.Name = dto.Name;
        category.Type = dto.Type;
        category.Icon = dto.Icon;
        category.Description = dto.Description;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, Guid userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return false;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }
}
