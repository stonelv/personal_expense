using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
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
        
        var response = accounts.Select(a => new AccountResponseDto(
            Id: a.Id,
            Name: a.Name,
            Type: a.Type,
            Balance: a.Balance,
            Description: a.Description,
            IsActive: a.IsActive,
            CreatedAt: a.CreatedAt,
            UpdatedAt: a.UpdatedAt
        ));

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountResponseDto>> GetAccount(Guid id)
    {
        var userId = GetCurrentUserId();
        var account = await _accountService.GetAccountByIdAsync(id, userId);

        var response = new AccountResponseDto(
            Id: account.Id,
            Name: account.Name,
            Type: account.Type,
            Balance: account.Balance,
            Description: account.Description,
            IsActive: account.IsActive,
            CreatedAt: account.CreatedAt,
            UpdatedAt: account.UpdatedAt
        );

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> PostAccount(AccountRequestDto accountDto)
    {
        var userId = GetCurrentUserId();
        var account = new Account
        {
            Name = accountDto.Name,
            Type = accountDto.Type,
            Balance = accountDto.Balance,
            Description = accountDto.Description
        };

        var createdAccount = await _accountService.CreateAccountAsync(account, userId);

        var response = new AccountResponseDto(
            Id: createdAccount.Id,
            Name: createdAccount.Name,
            Type: createdAccount.Type,
            Balance: createdAccount.Balance,
            Description: createdAccount.Description,
            IsActive: createdAccount.IsActive,
            CreatedAt: createdAccount.CreatedAt,
            UpdatedAt: createdAccount.UpdatedAt
        );

        return CreatedAtAction(nameof(GetAccount), new { id = createdAccount.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAccount(Guid id, AccountRequestDto accountDto)
    {
        var userId = GetCurrentUserId();
        var account = new Account
        {
            Name = accountDto.Name,
            Type = accountDto.Type,
            Balance = accountDto.Balance,
            Description = accountDto.Description
        };

        await _accountService.UpdateAccountAsync(id, account, userId);

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

