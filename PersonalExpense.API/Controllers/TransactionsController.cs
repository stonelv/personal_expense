using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using System;
using System.Security.Claims;

namespace PersonalExpense.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
    }

    [HttpGet]
    public async Task<ActionResult<TransactionListResponseDto>> GetTransactions(
        [FromQuery] int? year, 
        [FromQuery] int? month,
        [FromQuery] TransactionType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        
        var transactions = await _transactionService.GetTransactionsAsync(userId, year, month, type, page, pageSize);
        var totalCount = await _transactionService.GetTotalCountAsync(userId, year, month, type);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var items = transactions.Select(t => new TransactionResponseDto(
            Id: t.Id,
            Type: t.Type,
            Amount: t.Amount,
            TransactionDate: t.TransactionDate,
            Description: t.Description,
            AttachmentUrl: t.AttachmentUrl,
            CreatedAt: t.CreatedAt,
            UpdatedAt: t.UpdatedAt,
            AccountId: t.AccountId,
            AccountName: t.Account.Name,
            CategoryId: t.CategoryId,
            CategoryName: t.Category?.Name,
            TransferToAccountId: t.TransferToAccountId,
            TransferToAccountName: t.TransferToAccount?.Name
        ));

        var response = new TransactionListResponseDto(
            Items: items,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages
        );

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);

        var response = new TransactionResponseDto(
            Id: transaction.Id,
            Type: transaction.Type,
            Amount: transaction.Amount,
            TransactionDate: transaction.TransactionDate,
            Description: transaction.Description,
            AttachmentUrl: transaction.AttachmentUrl,
            CreatedAt: transaction.CreatedAt,
            UpdatedAt: transaction.UpdatedAt,
            AccountId: transaction.AccountId,
            AccountName: transaction.Account.Name,
            CategoryId: transaction.CategoryId,
            CategoryName: transaction.Category?.Name,
            TransferToAccountId: transaction.TransferToAccountId,
            TransferToAccountName: transaction.TransferToAccount?.Name
        );

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> PostTransaction(TransactionRequestDto transactionDto)
    {
        var userId = GetCurrentUserId();
        var transaction = new Transaction
        {
            Type = transactionDto.Type,
            Amount = transactionDto.Amount,
            TransactionDate = transactionDto.TransactionDate,
            Description = transactionDto.Description,
            AttachmentUrl = transactionDto.AttachmentUrl,
            AccountId = transactionDto.AccountId,
            CategoryId = transactionDto.CategoryId,
            TransferToAccountId = transactionDto.TransferToAccountId
        };

        var createdTransaction = await _transactionService.CreateTransactionAsync(transaction, userId);

        var response = new TransactionResponseDto(
            Id: createdTransaction.Id,
            Type: createdTransaction.Type,
            Amount: createdTransaction.Amount,
            TransactionDate: createdTransaction.TransactionDate,
            Description: createdTransaction.Description,
            AttachmentUrl: createdTransaction.AttachmentUrl,
            CreatedAt: createdTransaction.CreatedAt,
            UpdatedAt: createdTransaction.UpdatedAt,
            AccountId: createdTransaction.AccountId,
            AccountName: createdTransaction.Account.Name,
            CategoryId: createdTransaction.CategoryId,
            CategoryName: createdTransaction.Category?.Name,
            TransferToAccountId: createdTransaction.TransferToAccountId,
            TransferToAccountName: createdTransaction.TransferToAccount?.Name
        );

        return CreatedAtAction(nameof(GetTransaction), new { id = createdTransaction.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTransaction(Guid id, TransactionRequestDto transactionDto)
    {
        var userId = GetCurrentUserId();
        var transaction = new Transaction
        {
            Type = transactionDto.Type,
            Amount = transactionDto.Amount,
            TransactionDate = transactionDto.TransactionDate,
            Description = transactionDto.Description,
            AttachmentUrl = transactionDto.AttachmentUrl,
            AccountId = transactionDto.AccountId,
            CategoryId = transactionDto.CategoryId,
            TransferToAccountId = transactionDto.TransferToAccountId
        };

        await _transactionService.UpdateTransactionAsync(id, transaction, userId);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        await _transactionService.DeleteTransactionAsync(id, userId);

        return NoContent();
    }
}

