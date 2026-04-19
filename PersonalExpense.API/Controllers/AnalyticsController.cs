using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using System.Security.Claims;

namespace PersonalExpense.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
    }

    [HttpGet("trend")]
    public async Task<ActionResult<TrendAnalysisDto>> GetTrendAnalysis(
        [FromQuery] PeriodType periodType = PeriodType.Monthly,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int periodCount = 6)
    {
        var userId = GetCurrentUserId();
        var filter = new AnalyticsFilterParams
        {
            CategoryId = categoryId,
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _analyticsService.GetTrendAnalysisAsync(userId, periodType, filter, periodCount);
        return Ok(result);
    }

    [HttpGet("last-six-months")]
    public async Task<ActionResult<List<MonthlyTrendDto>>> GetLastSixMonthsTrend(
        [FromQuery] Guid? categoryId = null)
    {
        var userId = GetCurrentUserId();
        var result = await _analyticsService.GetLastSixMonthsTrendAsync(userId, categoryId);
        return Ok(result);
    }

    [HttpGet("category-breakdown")]
    public async Task<ActionResult<List<CategoryBreakdownDto>>> GetCategoryBreakdown(
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var userId = GetCurrentUserId();
        var targetDate = DateTime.UtcNow;
        var targetYear = year ?? targetDate.Year;
        var targetMonth = month ?? targetDate.Month;

        var result = await _analyticsService.GetCategoryBreakdownAsync(userId, targetYear, targetMonth);
        return Ok(result);
    }

    [HttpGet("monthly-detail/{year}/{month}")]
    public async Task<ActionResult<MonthlyDetailDto>> GetMonthlyDetail(
        [FromRoute] int year,
        [FromRoute] int month,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var result = await _analyticsService.GetMonthlyDetailAsync(
            userId,
            year,
            month,
            categoryId,
            pageNumber,
            pageSize);

        return Ok(result);
    }
}
