using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
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
        return await _context.Categories
            .Where(c => c.UserId == userId)
            .Select(c => new CategoryResponseDto(
                c.Id,
                c.Name,
                c.Type,
                c.Icon,
                c.Description,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .ToListAsync();
    }

    public async Task<CategoryResponseDto?> GetCategoryByIdAsync(Guid id, Guid userId)
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

    public async Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto categoryDto, Guid userId)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = categoryDto.Name,
            Type = categoryDto.Type,
            Icon = categoryDto.Icon,
            Description = categoryDto.Description,
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

    public async Task<CategoryResponseDto> UpdateCategoryAsync(Guid id, CategoryUpdateDto categoryDto, Guid userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
        {
            throw new InvalidOperationException("Category not found");
        }

        category.Name = categoryDto.Name;
        category.Type = categoryDto.Type;
        category.Icon = categoryDto.Icon;
        category.Description = categoryDto.Description;
        category.UpdatedAt = DateTime.UtcNow;

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

    public async Task DeleteCategoryAsync(Guid id, Guid userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
        {
            throw new InvalidOperationException("Category not found");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}
