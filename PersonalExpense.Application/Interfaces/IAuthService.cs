using PersonalExpense.Application.DTOs.Auth;

namespace PersonalExpense.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(RegisterDto registerDto);
    Task<(bool Succeeded, AuthResponseDto? Result, string Message)> LoginAsync(LoginDto loginDto);
}
