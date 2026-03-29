using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Application.DTOs.Transaction;

public class TransactionQueryParameters
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public TransactionType? Type { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
