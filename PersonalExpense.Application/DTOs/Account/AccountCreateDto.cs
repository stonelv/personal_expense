using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs.Account;

public class AccountCreateDto
{
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string? Description { get; set; }
}
