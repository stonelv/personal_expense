using PersonalExpense.Application.DTOs.Account;

namespace PersonalExpense.Application.Interfaces;

public interface IAccountService
{
    Task<List<AccountResponseDto>> GetAccountsAsync(Guid userId);
    Task<AccountResponseDto?> GetAccountByIdAsync(Guid id, Guid userId);
    Task<AccountResponseDto> CreateAccountAsync(Guid userId, AccountCreateDto dto);
    Task UpdateAccountAsync(Guid id, Guid userId, AccountUpdateDto dto);
    Task DeleteAccountAsync(Guid id, Guid userId);
}
