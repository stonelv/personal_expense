using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs.Account;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Application.Exceptions;
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
    public async Task<ActionResult<List<AccountResponseDto>>> GetAccounts()
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
            throw new NotFoundException("Account", id);
        }

        return Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> PostAccount(AccountCreateDto dto)
    {
        var userId = GetCurrentUserId();
        var account = await _accountService.CreateAccountAsync(userId, dto);
        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAccount(Guid id, AccountUpdateDto dto)
    {
        var userId = GetCurrentUserId();
        await _accountService.UpdateAccountAsync(id, userId, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var userId = GetCurrentUserId();
        await _accountService.DeleteAccountAsync(id, userId);
        return NoContent();
    }
}
