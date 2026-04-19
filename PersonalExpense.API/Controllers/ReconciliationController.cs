using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using System.Security.Claims;

namespace PersonalExpense.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReconciliationController : ControllerBase
{
    private readonly IReconciliationService _reconciliationService;

    public ReconciliationController(IReconciliationService reconciliationService)
    {
        _reconciliationService = reconciliationService;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
    }

    [HttpGet("account/{accountId}")]
    public async Task<ActionResult<ReconciliationResultDto>> ReconcileAccount([FromRoute] Guid accountId)
    {
        var userId = GetCurrentUserId();
        var result = await _reconciliationService.ReconcileAccountAsync(accountId, userId);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<ReconciliationResultDto>>> ReconcileAllAccounts()
    {
        var userId = GetCurrentUserId();
        var results = await _reconciliationService.ReconcileAllAccountsAsync(userId);
        return Ok(results);
    }

    [HttpGet("transfer-discrepancies")]
    public async Task<ActionResult<List<DiscrepancyItemDto>>> GetTransferDiscrepancies()
    {
        var userId = GetCurrentUserId();
        var results = await _reconciliationService.DetectTransferDiscrepanciesAsync(userId);
        return Ok(results);
    }
}
