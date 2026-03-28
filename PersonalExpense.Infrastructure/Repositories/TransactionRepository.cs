using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Domain.Enums;
using PersonalExpense.Domain.Interfaces;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Infrastructure.Repositories;

public interface ITransactionRepository : IUserOwnedRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByMonthAndUserIdAsync(int year, int month, int userId);
    Task<IEnumerable<Transaction>> GetByTypeAndUserIdAsync(TransactionType type, int userId);
    Task<IEnumerable<Transaction>> GetByCategoryAndUserIdAsync(int categoryId, int userId);
    Task<decimal> GetTotalExpenseByMonthAndUserIdAsync(int year, int month, int userId);
    Task<decimal> GetTotalExpenseByMonthCategoryAndUserIdAsync(int year, int month, int categoryId, int userId);
    Task<decimal> GetTotalIncomeByMonthAndUserIdAsync(int year, int month, int userId);
}

public class TransactionRepository : UserOwnedRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context)
    {
    }
    
    public override async Task<IEnumerable<Transaction>> GetAllByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
    
    public override async Task<Transaction?> GetByIdAndUserIdAsync(int id, int userId)
    {
        return await _dbSet
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    }
    
    public override async Task DeleteByIdAndUserIdAsync(int id, int userId)
    {
        var transaction = await GetByIdAndUserIdAsync(id, userId);
        if (transaction != null)
        {
            _dbSet.Remove(transaction);
        }
    }
    
    public async Task<IEnumerable<Transaction>> GetByMonthAndUserIdAsync(int year, int month, int userId)
    {
        return await _dbSet
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .Where(t => t.UserId == userId && t.Date.Year == year && t.Date.Month == month)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Transaction>> GetByTypeAndUserIdAsync(TransactionType type, int userId)
    {
        return await _dbSet
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .Where(t => t.UserId == userId && t.Type == type)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Transaction>> GetByCategoryAndUserIdAsync(int categoryId, int userId)
    {
        return await _dbSet
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .Where(t => t.UserId == userId && t.CategoryId == categoryId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
    
    public async Task<decimal> GetTotalExpenseByMonthAndUserIdAsync(int year, int month, int userId)
    {
        return await _dbSet
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense 
                      && t.Date.Year == year && t.Date.Month == month)
            .SumAsync(t => t.Amount);
    }
    
    public async Task<decimal> GetTotalExpenseByMonthCategoryAndUserIdAsync(int year, int month, int categoryId, int userId)
    {
        return await _dbSet
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense 
                      && t.Date.Year == year && t.Date.Month == month 
                      && t.CategoryId == categoryId)
            .SumAsync(t => t.Amount);
    }
    
    public async Task<decimal> GetTotalIncomeByMonthAndUserIdAsync(int year, int month, int userId)
    {
        return await _dbSet
            .Where(t => t.UserId == userId && t.Type == TransactionType.Income 
                      && t.Date.Year == year && t.Date.Month == month)
            .SumAsync(t => t.Amount);
    }
}