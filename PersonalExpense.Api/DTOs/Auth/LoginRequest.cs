using System.ComponentModel.DataAnnotations;

namespace PersonalExpense.Api.DTOs.Auth;

public class LoginRequest
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}