using Microsoft.IdentityModel.Tokens;
using PersonalExpense.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace PersonalExpense.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message)> RegisterAsync(string email, string password);
    Task<(bool Success, string Token, System.DateTime Expiration)> LoginAsync(string email, string password);
    JwtSecurityToken GenerateJwtToken(User user);
}
