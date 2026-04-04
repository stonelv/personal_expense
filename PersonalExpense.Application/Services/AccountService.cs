using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace PersonalExpense.Application.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;

    public AccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Account>> GetAccountsAsync(Guid userId)
    {
        return await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
    }

    public async Task<Account> GetAccountByIdAsync(Guid id, Guid userId)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (account == null)
        {
            throw new KeyNotFoundException("Account not found");
        }
        return account;
    }

    public async Task<Account> CreateAccountAsync(Account account, Guid userId)
    {
        account.UserId = userId;
        account.IsActive = true;
        account.CreatedAt = DateTime.UtcNow;

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return account;
    }

    public async Task UpdateAccountAsync(Guid id, Account account, Guid userId)
    {
        var existingAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (existingAccount == null)
        {
            throw new KeyNotFoundException("Account not found");
        }

        existingAccount.Name = account.Name;
        existingAccount.Type = account.Type;
        existingAccount.Balance = account.Balance;
        existingAccount.Description = account.Description;
        existingAccount.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(Guid id, Guid userId)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (account == null)
        {
            throw new KeyNotFoundException("Account not found");
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
    }
}
