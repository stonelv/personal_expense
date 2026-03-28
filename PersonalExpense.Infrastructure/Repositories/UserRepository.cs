using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Domain.Interfaces;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Infrastructure.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }
    
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
    }
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        return await _dbSet.FirstOrDefaultAsync(u => 
            u.Username == usernameOrEmail || u.Email == usernameOrEmail);
    }
}