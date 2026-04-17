namespace PersonalExpense.Application.DTOs;

public record RegisterDto(
    string Email,
    string Password,
    string ConfirmPassword
);

public record LoginDto(
    string Email,
    string Password
);

public record AuthResultDto(
    bool Success,
    string? Token,
    DateTime? Expiration,
    List<string> Errors
);
