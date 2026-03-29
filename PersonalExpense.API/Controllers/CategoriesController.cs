using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs.Category;
using PersonalExpense.Application.Interfaces;
using System.Security.Claims;

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

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetCategories()
    {
        var userId = GetCurrentUserId();
        var categories = await _categoryService.GetCategoriesAsync(userId);
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryResponseDto>> GetCategory(Guid id)
    {
        var userId = GetCurrentUserId();
        var category = await _categoryService.GetCategoryByIdAsync(id, userId);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> PostCategory(CategoryCreateDto categoryDto)
    {
        var userId = GetCurrentUserId();
        var category = await _categoryService.CreateCategoryAsync(userId, categoryDto);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutCategory(Guid id, CategoryUpdateDto categoryDto)
    {
        var userId = GetCurrentUserId();
        await _categoryService.UpdateCategoryAsync(id, userId, categoryDto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var userId = GetCurrentUserId();
        await _categoryService.DeleteCategoryAsync(id, userId);
        return NoContent();
    }
}
