using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Api.DTOs.Category;
using PersonalExpense.Api.Extensions;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Domain.Enums;
using PersonalExpense.Infrastructure.Repositories;

namespace PersonalExpense.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;
    
    public CategoriesController(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CategoryType? type)
    {
        var userId = this.GetCurrentUserId();
        IEnumerable<Category> categories;
        
        if (type.HasValue)
        {
            categories = await _categoryRepository.GetByTypeAndUserIdAsync(type.Value, userId);
        }
        else
        {
            categories = await _categoryRepository.GetAllByUserIdAsync(userId);
        }
        
        var categoryDtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Type = c.Type,
            Icon = c.Icon,
            Color = c.Color,
            Description = c.Description,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();
        
        return Ok(categoryDtos);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = this.GetCurrentUserId();
        var category = await _categoryRepository.GetByIdAndUserIdAsync(id, userId);
        
        if (category == null)
        {
            return NotFound();
        }
        
        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            Icon = category.Icon,
            Color = category.Color,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
        
        return Ok(categoryDto);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request)
    {
        var userId = this.GetCurrentUserId();
        
        if (await _categoryRepository.GetByNameAndUserIdAsync(request.Name, userId) != null)
        {
            return BadRequest("Category with this name already exists");
        }
        
        var category = new Category
        {
            Name = request.Name,
            Type = request.Type,
            Icon = request.Icon,
            Color = request.Color,
            Description = request.Description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();
        
        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            Icon = category.Icon,
            Color = category.Color,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
        
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, categoryDto);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest request)
    {
        var userId = this.GetCurrentUserId();
        var category = await _categoryRepository.GetByIdAndUserIdAsync(id, userId);
        
        if (category == null)
        {
            return NotFound();
        }
        
        if (category.Name != request.Name && 
            await _categoryRepository.GetByNameAndUserIdAsync(request.Name, userId) != null)
        {
            return BadRequest("Category with this name already exists");
        }
        
        category.Name = request.Name;
        category.Type = request.Type;
        category.Icon = request.Icon;
        category.Color = request.Color;
        category.Description = request.Description;
        category.UpdatedAt = DateTime.UtcNow;
        
        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();
        
        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            Icon = category.Icon,
            Color = category.Color,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
        
        return Ok(categoryDto);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.GetCurrentUserId();
        await _categoryRepository.DeleteByIdAndUserIdAsync(id, userId);
        await _categoryRepository.SaveChangesAsync();
        
        return NoContent();
    }
}