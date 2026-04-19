using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;

namespace PersonalExpense.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var userId = this.GetCurrentUserIdOrThrow();
        var categories = await _categoryService.GetCategoriesAsync(userId);
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(Guid id)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        var category = await _categoryService.GetCategoryByIdAsync(id, userId);
        
        if (category == null)
        {
            throw new NotFoundException(nameof(Category), id);
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> PostCategory(CategoryCreateDto dto)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        var category = await _categoryService.CreateCategoryAsync(dto, userId);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutCategory(Guid id, CategoryUpdateDto dto)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        await _categoryService.UpdateCategoryAsync(id, dto, userId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var userId = this.GetCurrentUserIdOrThrow();
        await _categoryService.DeleteCategoryAsync(id, userId);
        return NoContent();
    }
}
