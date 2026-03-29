using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
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

    public async Task<IEnumerable<AccountResponseDto>> GetAccountsAsync(Guid userId)
    {
        return await _context.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new AccountResponseDto(
                a.Id,
                a.Name,
                a.Type,
                a.Balance,
                a.Description,
                a.IsActive,
                a.CreatedAt,
                a.UpdatedAt
            ))
            .ToListAsync();
    }

    public async Task<AccountResponseDto?> GetAccountByIdAsync(Guid id, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
            return null;

        return new AccountResponseDto(
            account.Id,
            account.Name,
            account.Type,
            account.Balance,
            account.Description,
            account.IsActive,
            account.CreatedAt,
            account.UpdatedAt
        );
    }

    public async Task<AccountResponseDto> CreateAccountAsync(AccountCreateDto accountDto, Guid userId)
    {
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

        return new AccountResponseDto(
            account.Id,
            account.Name,
            account.Type,
            account.Balance,
            account.Description,
            account.IsActive,
            account.CreatedAt,
            account.UpdatedAt
        );
    }

    public async Task<AccountResponseDto> UpdateAccountAsync(Guid id, AccountUpdateDto accountDto, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            throw new InvalidOperationException("Account not found");
        }

        account.Name = accountDto.Name;
        account.Type = accountDto.Type;
        account.Balance = accountDto.Balance;
        account.Description = accountDto.Description;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new AccountResponseDto(
            account.Id,
            account.Name,
            account.Type,
            account.Balance,
            account.Description,
            account.IsActive,
            account.CreatedAt,
            account.UpdatedAt
        );
    }

    public async Task DeleteAccountAsync(Guid id, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            throw new InvalidOperationException("Account not found");
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
    }
}
