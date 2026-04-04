namespace PersonalExpense.Application.DTOs;

public record RegisterRequestDto(
    string Email,
    string Password,
    string ConfirmPassword
);

public record LoginRequestDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token,
    DateTime Expiration,
    string Message
);
