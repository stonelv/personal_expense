namespace PersonalExpense.Application.DTOs.Auth;

public record RegisterDto(string Email, string Password, string ConfirmPassword);
