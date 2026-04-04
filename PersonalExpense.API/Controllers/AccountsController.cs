using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using System.Security.Claims;

namespace PersonalExpense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAccounts()
    {
        var userId = GetCurrentUserId();
        var accounts = await _accountService.GetAccountsAsync(userId);
        return Ok(accounts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountResponseDto>> GetAccount(Guid id)
    {
        var userId = GetCurrentUserId();
        var account = await _accountService.GetAccountAsync(id, userId);

        if (account == null)
        {
            return NotFound();
        }

        return Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> PostAccount(CreateAccountDto dto)
    {
        var userId = GetCurrentUserId();
        var account = await _accountService.CreateAccountAsync(dto, userId);
        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAccount(Guid id, UpdateAccountDto dto)
    {
        var userId = GetCurrentUserId();
        var success = await _accountService.UpdateAccountAsync(id, dto, userId);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var userId = GetCurrentUserId();
        var success = await _accountService.DeleteAccountAsync(id, userId);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
