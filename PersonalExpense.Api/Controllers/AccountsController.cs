using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Api.DTOs.Account;
using PersonalExpense.Api.Extensions;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Repositories;

namespace PersonalExpense.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountRepository _accountRepository;
    
    public AccountsController(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = this.GetCurrentUserId();
        var accounts = await _accountRepository.GetAllByUserIdAsync(userId);
        
        var accountDtos = accounts.Select(a => new AccountDto
        {
            Id = a.Id,
            Name = a.Name,
            Type = a.Type,
            Balance = a.Balance,
            Description = a.Description,
            Currency = a.Currency,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();
        
        return Ok(accountDtos);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = this.GetCurrentUserId();
        var account = await _accountRepository.GetByIdAndUserIdAsync(id, userId);
        
        if (account == null)
        {
            return NotFound();
        }
        
        var accountDto = new AccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            Balance = account.Balance,
            Description = account.Description,
            Currency = account.Currency,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
        
        return Ok(accountDto);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request)
    {
        var userId = this.GetCurrentUserId();
        
        if (await _accountRepository.GetByNameAndUserIdAsync(request.Name, userId) != null)
        {
            return BadRequest("Account with this name already exists");
        }
        
        var account = new Account
        {
            Name = request.Name,
            Type = request.Type,
            Balance = request.InitialBalance,
            Description = request.Description,
            Currency = request.Currency,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        await _accountRepository.AddAsync(account);
        await _accountRepository.SaveChangesAsync();
        
        var accountDto = new AccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            Balance = account.Balance,
            Description = account.Description,
            Currency = account.Currency,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
        
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, accountDto);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateAccountRequest request)
    {
        var userId = this.GetCurrentUserId();
        var account = await _accountRepository.GetByIdAndUserIdAsync(id, userId);
        
        if (account == null)
        {
            return NotFound();
        }
        
        if (account.Name != request.Name && 
            await _accountRepository.GetByNameAndUserIdAsync(request.Name, userId) != null)
        {
            return BadRequest("Account with this name already exists");
        }
        
        account.Name = request.Name;
        account.Type = request.Type;
        account.Description = request.Description;
        account.Currency = request.Currency;
        account.UpdatedAt = DateTime.UtcNow;
        
        await _accountRepository.UpdateAsync(account);
        await _accountRepository.SaveChangesAsync();
        
        var accountDto = new AccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            Balance = account.Balance,
            Description = account.Description,
            Currency = account.Currency,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
        
        return Ok(accountDto);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.GetCurrentUserId();
        await _accountRepository.DeleteByIdAndUserIdAsync(id, userId);
        await _accountRepository.SaveChangesAsync();
        
        return NoContent();
    }
}