namespace PersonalExpense.Domain.Interfaces;

public interface IUserOwnedRepository<T> : IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllByUserIdAsync(int userId);
    Task<T?> GetByIdAndUserIdAsync(int id, int userId);
    Task DeleteByIdAndUserIdAsync(int id, int userId);
}