using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Api.Services;

public interface IJwtTokenGenerator
{
    (string Token, DateTime Expiration) GenerateToken(User user);
}