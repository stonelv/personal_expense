using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
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
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return accounts.Select(a => new AccountResponseDto(
            a.Id,
            a.Name,
            a.Type,
            a.Balance,
            a.Description,
            a.IsActive,
            a.CreatedAt,
            a.UpdatedAt
        ));
    }

    public async Task<AccountResponseDto?> GetAccountAsync(Guid id, Guid userId)
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

    public async Task<AccountResponseDto> CreateAccountAsync(CreateAccountDto dto, Guid userId)
    {
        var account = new Domain.Entities.Account
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

    public async Task<bool> UpdateAccountAsync(Guid id, UpdateAccountDto dto, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
            return false;

        account.Name = dto.Name;
        account.Type = dto.Type;
        account.Balance = dto.Balance;
        account.Description = dto.Description;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAccountAsync(Guid id, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
            return false;

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
        return true;
    }
}
