using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using System.Security.Claims;

namespace PersonalExpense.API.Controllers;

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
        var account = await _accountService.GetAccountByIdAsync(id, userId);
        
        if (account == null)
        {
            return NotFound();
        }

        return Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> PostAccount(AccountCreateDto accountDto)
    {
        var userId = GetCurrentUserId();
        var result = await _accountService.CreateAccountAsync(accountDto, userId);
        return CreatedAtAction(nameof(GetAccount), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AccountResponseDto>> PutAccount(Guid id, AccountUpdateDto accountDto)
    {
        var userId = GetCurrentUserId();
        var result = await _accountService.UpdateAccountAsync(id, accountDto, userId);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var userId = GetCurrentUserId();
        await _accountService.DeleteAccountAsync(id, userId);
        return NoContent();
    }
}
