using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountResponseDto>> GetAccountsAsync(Guid userId);
    Task<AccountResponseDto?> GetAccountAsync(Guid id, Guid userId);
    Task<AccountResponseDto> CreateAccountAsync(CreateAccountDto dto, Guid userId);
    Task<bool> UpdateAccountAsync(Guid id, UpdateAccountDto dto, Guid userId);
    Task<bool> DeleteAccountAsync(Guid id, Guid userId);
}
