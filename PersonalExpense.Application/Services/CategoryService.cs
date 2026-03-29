using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs.Category;
using PersonalExpense.Application.Exceptions;
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
        return await _context.Categories
            .Where(c => c.UserId == userId)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                Icon = c.Icon,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<CategoryResponseDto?> GetCategoryByIdAsync(Guid id, Guid userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
        {
            return null;
        }

        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            Icon = category.Icon,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    public async Task<CategoryResponseDto> CreateCategoryAsync(Guid userId, CategoryCreateDto dto)
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

        return await GetCategoryByIdAsync(category.Id, userId) ?? throw new NotFoundException("Category not found after creation");
    }

    public async Task UpdateCategoryAsync(Guid id, Guid userId, CategoryUpdateDto dto)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        category.Name = dto.Name;
        category.Type = dto.Type;
        category.Icon = dto.Icon;
        category.Description = dto.Description;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(Guid id, Guid userId)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}
