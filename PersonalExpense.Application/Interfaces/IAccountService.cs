using PersonalExpense.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PersonalExpense.Application.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<Account>> GetAccountsAsync(Guid userId);
    Task<Account> GetAccountByIdAsync(Guid id, Guid userId);
    Task<Account> CreateAccountAsync(Account account, Guid userId);
    Task UpdateAccountAsync(Guid id, Account account, Guid userId);
    Task DeleteAccountAsync(Guid id, Guid userId);
}
