using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Interfaces;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Infrastructure.Repositories;

public abstract class UserOwnedRepository<T> : Repository<T>, IUserOwnedRepository<T> 
    where T : class
{
    protected UserOwnedRepository(AppDbContext context) : base(context)
    {
    }
    
    public abstract Task<IEnumerable<T>> GetAllByUserIdAsync(int userId);
    public abstract Task<T?> GetByIdAndUserIdAsync(int id, int userId);
    public abstract Task DeleteByIdAndUserIdAsync(int id, int userId);
}