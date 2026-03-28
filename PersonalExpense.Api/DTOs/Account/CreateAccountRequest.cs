using PersonalExpense.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PersonalExpense.Api.DTOs.Account;

public class CreateAccountRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public AccountType Type { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal InitialBalance { get; set; } = 0;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(10)]
    public string? Currency { get; set; } = "CNY";
}