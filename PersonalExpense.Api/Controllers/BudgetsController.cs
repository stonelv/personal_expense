using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Api.DTOs.Budget;
using PersonalExpense.Api.Extensions;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Repositories;

namespace PersonalExpense.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;

    public BudgetsController(IBudgetRepository budgetRepository, ICategoryRepository categoryRepository)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = this.GetCurrentUserId();
        var budgets = await _budgetRepository.GetAllByUserIdAsync(userId);

        var budgetDtos = budgets.Select(b => new BudgetDto
        {
            Id = b.Id,
            Month = b.Month,
            Year = b.Year,
            Amount = b.Amount,
            CategoryId = b.CategoryId,
            CategoryName = b.Category?.Name,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        }).ToList();

        return Ok(budgetDtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = this.GetCurrentUserId();
        var budget = await _budgetRepository.GetByIdAndUserIdAsync(id, userId);

        if (budget == null)
        {
            return NotFound();
        }

        var budgetDto = new BudgetDto
        {
            Id = budget.Id,
            Month = budget.Month,
            Year = budget.Year,
            Amount = budget.Amount,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category?.Name,
            CreatedAt = budget.CreatedAt,
            UpdatedAt = budget.UpdatedAt
        };

        return Ok(budgetDto);
    }

    [HttpGet("month/{month}/year/{year}")]
    public async Task<IActionResult> GetByMonthYear(int month, int year, bool includeCategory = true)
    {
        var userId = this.GetCurrentUserId();
        var budgets = await _budgetRepository.GetByMonthYearAndUserIdAsync(month, year, userId, includeCategory);

        var budgetDtos = budgets.Select(b => new BudgetDto
        {
            Id = b.Id,
            Month = b.Month,
            Year = b.Year,
            Amount = b.Amount,
            CategoryId = b.CategoryId,
            CategoryName = b.Category?.Name,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        }).ToList();

        return Ok(budgetDtos);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBudgetRequest request)
    {
        var userId = this.GetCurrentUserId();

        // 验证月份和年份
        if (request.Month < 1 || request.Month > 12)
        {
            return BadRequest("Month must be between 1 and 12");
        }

        if (request.CategoryId.HasValue)
        {
            // 验证分类是否存在且属于当前用户
            var category = await _categoryRepository.GetByIdAndUserIdAsync(request.CategoryId.Value, userId);
            if (category == null)
            {
                return BadRequest("Category not found");
            }

            // 检查该月份该分类是否已有预算
            if (await _budgetRepository.GetByMonthYearCategoryAndUserIdAsync(request.Month, request.Year, request.CategoryId.Value, userId) != null)
            {
                return BadRequest("Budget for this category in this month already exists");
            }
        }
        else
        {
            // 检查该月份总额预算是否已存在
            if (await _budgetRepository.GetByMonthYearAndUserIdAsync(request.Month, request.Year, userId) != null)
            {
                return BadRequest("Total budget for this month already exists");
            }
        }

        var budget = new Budget
        {
            Month = request.Month,
            Year = request.Year,
            Amount = request.Amount,
            CategoryId = request.CategoryId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _budgetRepository.AddAsync(budget);
        await _budgetRepository.SaveChangesAsync();

        var budgetDto = new BudgetDto
        {
            Id = budget.Id,
            Month = budget.Month,
            Year = budget.Year,
            Amount = budget.Amount,
            CategoryId = budget.CategoryId,
            CreatedAt = budget.CreatedAt,
            UpdatedAt = budget.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = budget.Id }, budgetDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateBudgetRequest request)
    {
        var userId = this.GetCurrentUserId();
        var budget = await _budgetRepository.GetByIdAndUserIdAsync(id, userId);

        if (budget == null)
        {
            return NotFound();
        }

        // 验证月份和年份
        if (request.Month < 1 || request.Month > 12)
        {
            return BadRequest("Month must be between 1 and 12");
        }

        if (request.CategoryId.HasValue)
        {
            // 验证分类是否存在且属于当前用户
            var category = await _categoryRepository.GetByIdAndUserIdAsync(request.CategoryId.Value, userId);
            if (category == null)
            {
                return BadRequest("Category not found");
            }
        }

        // 检查是否有冲突的预算
        if (request.CategoryId.HasValue)
        {
            var existingBudget = await _budgetRepository.GetByMonthYearCategoryAndUserIdAsync(request.Month, request.Year, request.CategoryId.Value, userId);
            if (existingBudget != null && existingBudget.Id != id)
            {
                return BadRequest("Budget for this category in this month already exists");
            }
        }
        else
        {
            var existingBudget = await _budgetRepository.GetByMonthYearAndUserIdAsync(request.Month, request.Year, userId);
            if (existingBudget != null && existingBudget.Id != id)
            {
                return BadRequest("Total budget for this month already exists");
            }
        }

        budget.Month = request.Month;
        budget.Year = request.Year;
        budget.Amount = request.Amount;
        budget.CategoryId = request.CategoryId;
        budget.UpdatedAt = DateTime.UtcNow;

        await _budgetRepository.UpdateAsync(budget);
        await _budgetRepository.SaveChangesAsync();

        var budgetDto = new BudgetDto
        {
            Id = budget.Id,
            Month = budget.Month,
            Year = budget.Year,
            Amount = budget.Amount,
            CategoryId = budget.CategoryId,
            CreatedAt = budget.CreatedAt,
            UpdatedAt = budget.UpdatedAt
        };

        return Ok(budgetDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.GetCurrentUserId();
        await _budgetRepository.DeleteByIdAndUserIdAsync(id, userId);
        await _budgetRepository.SaveChangesAsync();

        return NoContent();
    }
}
