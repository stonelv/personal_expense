using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Domain.Interfaces;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Infrastructure.Repositories;

public interface IBudgetRepository : IUserOwnedRepository<Budget>
{
    Task<Budget?> GetByMonthYearAndUserIdAsync(int month, int year, int userId);
    Task<Budget?> GetByMonthYearCategoryAndUserIdAsync(int month, int year, int categoryId, int userId);
    Task<IEnumerable<Budget>> GetByMonthYearAndUserIdAsync(int month, int year, int userId, bool includeCategory);
    Task<decimal> GetTotalBudgetByMonthYearAndUserIdAsync(int month, int year, int userId);
}

public class BudgetRepository : UserOwnedRepository<Budget>, IBudgetRepository
{
    public BudgetRepository(AppDbContext context) : base(context)
    {
    }
    
    public override async Task<IEnumerable<Budget>> GetAllByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(b => b.Category)
            .Where(b => b.UserId == userId)
            .ToListAsync();
    }
    
    public override async Task<Budget?> GetByIdAndUserIdAsync(int id, int userId)
    {
        return await _dbSet
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
    }
    
    public override async Task DeleteByIdAndUserIdAsync(int id, int userId)
    {
        var budget = await GetByIdAndUserIdAsync(id, userId);
        if (budget != null)
        {
            _dbSet.Remove(budget);
        }
    }
    
    public async Task<Budget?> GetByMonthYearAndUserIdAsync(int month, int year, int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == month && b.Year == year && b.CategoryId == null);
    }
    
    public async Task<Budget?> GetByMonthYearCategoryAndUserIdAsync(int month, int year, int categoryId, int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == month && b.Year == year && b.CategoryId == categoryId);
    }
    
    public async Task<IEnumerable<Budget>> GetByMonthYearAndUserIdAsync(int month, int year, int userId, bool includeCategory)
    {
        var query = _dbSet.Where(b => b.UserId == userId && b.Month == month && b.Year == year);
        
        if (includeCategory)
        {
            query = query.Include(b => b.Category);
        }
        
        return await query.ToListAsync();
    }
    
    public async Task<decimal> GetTotalBudgetByMonthYearAndUserIdAsync(int month, int year, int userId)
    {
        return await _dbSet
            .Where(b => b.UserId == userId && b.Month == month && b.Year == year)
            .SumAsync(b => b.Amount);
    }
}