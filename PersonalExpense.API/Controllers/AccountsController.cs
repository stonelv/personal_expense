using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;
using System.Security.Claims;

namespace PersonalExpense.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AccountsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
    {
        var userId = GetCurrentUserId();
        return await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Account>> GetAccount(Guid id)
    {
        var userId = GetCurrentUserId();
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            return NotFound();
        }

        return account;
    }

    [HttpPost]
    public async Task<ActionResult<Account>> PostAccount(AccountDto accountDto)
    {
        var userId = GetCurrentUserId();
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = accountDto.Name,
            Type = accountDto.Type,
            Balance = accountDto.Balance,
            Description = accountDto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAccount(Guid id, AccountDto accountDto)
    {
        var userId = GetCurrentUserId();
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            return NotFound();
        }

        account.Name = accountDto.Name;
        account.Type = accountDto.Type;
        account.Balance = accountDto.Balance;
        account.Description = accountDto.Description;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var userId = GetCurrentUserId();
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            return NotFound();
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record AccountDto(string Name, AccountType Type, decimal Balance, string? Description);
