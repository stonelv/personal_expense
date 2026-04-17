using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface IAccountService
{
    Task<List<AccountDto>> GetAccountsAsync(Guid userId);
    Task<AccountDto?> GetAccountByIdAsync(Guid id, Guid userId);
    Task<AccountDto> CreateAccountAsync(AccountCreateDto dto, Guid userId);
    Task<AccountDto> UpdateAccountAsync(Guid id, AccountUpdateDto dto, Guid userId);
    Task DeleteAccountAsync(Guid id, Guid userId);
}
