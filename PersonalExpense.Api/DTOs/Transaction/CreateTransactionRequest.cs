using PersonalExpense.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PersonalExpense.Api.DTOs.Transaction;

public class CreateTransactionRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [Required]
    public TransactionType Type { get; set; }
    
    public DateTime? Date { get; set; }
    
    [StringLength(1000)]
    public string? Note { get; set; }
    
    [Url]
    [StringLength(500)]
    public string? AttachmentUrl { get; set; }
    
    [Required]
    public int AccountId { get; set; }
    
    // Required for Expense and Income
    public int? CategoryId { get; set; }
    
    // Required for Transfer
    public int? ToAccountId { get; set; }
}