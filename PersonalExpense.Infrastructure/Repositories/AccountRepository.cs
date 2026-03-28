using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Domain.Interfaces;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Infrastructure.Repositories;

public interface IAccountRepository : IUserOwnedRepository<Account>
{
    Task<Account?> GetByNameAndUserIdAsync(string name, int userId);
    Task<decimal> GetTotalBalanceByUserIdAsync(int userId);
}

public class AccountRepository : UserOwnedRepository<Account>, IAccountRepository
{
    public AccountRepository(AppDbContext context) : base(context)
    {
    }
    
    public override async Task<IEnumerable<Account>> GetAllByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }
    
    public override async Task<Account?> GetByIdAndUserIdAsync(int id, int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }
    
    public override async Task DeleteByIdAndUserIdAsync(int id, int userId)
    {
        var account = await GetByIdAndUserIdAsync(id, userId);
        if (account != null)
        {
            _dbSet.Remove(account);
        }
    }
    
    public async Task<Account?> GetByNameAndUserIdAsync(string name, int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.Name == name && a.UserId == userId);
    }
    
    public async Task<decimal> GetTotalBalanceByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .SumAsync(a => a.Balance);
    }
}