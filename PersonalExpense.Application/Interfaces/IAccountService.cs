using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountResponseDto>> GetAccountsAsync(Guid userId);
    Task<AccountResponseDto?> GetAccountByIdAsync(Guid id, Guid userId);
    Task<AccountResponseDto> CreateAccountAsync(AccountCreateDto accountDto, Guid userId);
    Task<AccountResponseDto> UpdateAccountAsync(Guid id, AccountUpdateDto accountDto, Guid userId);
    Task DeleteAccountAsync(Guid id, Guid userId);
}
