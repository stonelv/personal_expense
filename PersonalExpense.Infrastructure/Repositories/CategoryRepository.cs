using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Domain.Enums;
using PersonalExpense.Domain.Interfaces;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Infrastructure.Repositories;

public interface ICategoryRepository : IUserOwnedRepository<Category>
{
    Task<Category?> GetByNameAndUserIdAsync(string name, int userId);
    Task<IEnumerable<Category>> GetByTypeAndUserIdAsync(CategoryType type, int userId);
}

public class CategoryRepository : UserOwnedRepository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context)
    {
    }
    
    public override async Task<IEnumerable<Category>> GetAllByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }
    
    public override async Task<Category?> GetByIdAndUserIdAsync(int id, int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }
    
    public override async Task DeleteByIdAndUserIdAsync(int id, int userId)
    {
        var category = await GetByIdAndUserIdAsync(id, userId);
        if (category != null)
        {
            _dbSet.Remove(category);
        }
    }
    
    public async Task<Category?> GetByNameAndUserIdAsync(string name, int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Name == name && c.UserId == userId);
    }
    
    public async Task<IEnumerable<Category>> GetByTypeAndUserIdAsync(CategoryType type, int userId)
    {
        return await _dbSet
            .Where(c => c.Type == type && c.UserId == userId)
            .ToListAsync();
    }
}