using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs.Account;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;

    public AccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountResponseDto>> GetAccountsAsync(Guid userId)
    {
        return await _context.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new AccountResponseDto
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type,
                Balance = a.Balance,
                Description = a.Description,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<AccountResponseDto?> GetAccountByIdAsync(Guid id, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            return null;
        }

        return new AccountResponseDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            Balance = account.Balance,
            Description = account.Description,
            IsActive = account.IsActive,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    public async Task<AccountResponseDto> CreateAccountAsync(Guid userId, AccountCreateDto dto)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Type = dto.Type,
            Balance = dto.Balance,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return await GetAccountByIdAsync(account.Id, userId) ?? throw new NotFoundException("Account not found after creation");
    }

    public async Task UpdateAccountAsync(Guid id, Guid userId, AccountUpdateDto dto)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            throw new NotFoundException("Account", id);
        }

        account.Name = dto.Name;
        account.Type = dto.Type;
        account.Balance = dto.Balance;
        account.Description = dto.Description;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(Guid id, Guid userId)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            throw new NotFoundException("Account", id);
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
    }
}
