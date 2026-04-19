using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class ReconciliationService : IReconciliationService
{
    private readonly ApplicationDbContext _context;

    public ReconciliationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReconciliationResultDto> ReconcileAccountAsync(Guid accountId, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
        
        if (account == null)
        {
            throw new NotFoundException(nameof(Account), accountId);
        }

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && t.AccountId == accountId)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();

        var discrepancies = new List<DiscrepancyItemDto>();

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var legacyTransferOut = transactions
            .Where(t => t.Type == TransactionType.Transfer && t.TransferToAccountId != accountId)
            .Sum(t => t.Amount);

        var legacyTransferIn = transactions
            .Where(t => t.Type == TransactionType.Transfer && t.TransferToAccountId == accountId)
            .Sum(t => t.Amount);

        var calculatedNetChange = totalIncome - totalExpense - legacyTransferOut + legacyTransferIn;

        var relatedTransactions = transactions
            .Where(t => t.RelatedTransactionId.HasValue)
            .ToList();

        foreach (var trans in relatedTransactions)
        {
            var related = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == trans.RelatedTransactionId && t.UserId == userId);
            
            if (related == null)
            {
                discrepancies.Add(new DiscrepancyItemDto(
                    Type: "OrphanTransfer",
                    Description: $"Transaction {trans.Id} has a missing related transaction",
                    Expected: 0,
                    Actual: 1,
                    Difference: 1
                ));
            }
            else if (Math.Abs(related.Amount - trans.Amount) > 0.001m)
            {
                discrepancies.Add(new DiscrepancyItemDto(
                    Type: "AmountMismatch",
                    Description: $"Transfer amounts mismatch between transactions {trans.Id} and {related.Id}",
                    Expected: trans.Amount,
                    Actual: related.Amount,
                    Difference: related.Amount - trans.Amount
                ));
            }
        }

        return new ReconciliationResultDto(
            AccountId: account.Id,
            AccountName: account.Name,
            AccountType: account.Type,
            IsBalanced: discrepancies.Count == 0,
            ExpectedBalance: calculatedNetChange,
            ActualBalance: account.Balance,
            Discrepancy: 0,
            Discrepancies: discrepancies,
            ReconciliationDate: DateTime.UtcNow
        );
    }

    public async Task<List<ReconciliationResultDto>> ReconcileAllAccountsAsync(Guid userId)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var results = new List<ReconciliationResultDto>();

        foreach (var account in accounts)
        {
            var result = await ReconcileAccountAsync(account.Id, userId);
            results.Add(result);
        }

        return results;
    }

    public async Task<List<DiscrepancyItemDto>> DetectTransferDiscrepanciesAsync(Guid userId)
    {
        var discrepancies = new List<DiscrepancyItemDto>();

        var allTransactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();

        var transferTransactions = allTransactions
            .Where(t => t.RelatedTransactionId.HasValue)
            .ToList();

        var transferIds = transferTransactions
            .Select(t => t.RelatedTransactionId.Value)
            .Distinct()
            .ToList();

        foreach (var transferId in transferIds)
        {
            var related = allTransactions.FirstOrDefault(t => t.Id == transferId);
            if (related == null)
            {
                var referencing = transferTransactions.First(t => t.RelatedTransactionId == transferId);
                discrepancies.Add(new DiscrepancyItemDto(
                    Type: "MissingRelated",
                    Description: $"Missing related transaction for {referencing.Id}",
                    Expected: 1,
                    Actual: 0,
                    Difference: -1
                ));
            }
        }

        var pairedTransfers = allTransactions
            .Where(t => t.Type == TransactionType.Transfer && !t.RelatedTransactionId.HasValue)
            .ToList();

        foreach (var transfer in pairedTransfers)
        {
            if (transfer.TransferToAccountId.HasValue)
            {
                var counterPart = allTransactions.FirstOrDefault(t =>
                    t.Type == TransactionType.Transfer &&
                    t.AccountId == transfer.TransferToAccountId.Value &&
                    t.TransferToAccountId == transfer.AccountId &&
                    Math.Abs(t.Amount - transfer.Amount) < 0.001m);

                if (counterPart == null)
                {
                    discrepancies.Add(new DiscrepancyItemDto(
                        Type: "UnpairedTransfer",
                        Description: $"Legacy transfer {transfer.Id} has no counterpart",
                        Expected: 1,
                        Actual: 0,
                        Difference: -1
                    ));
                }
            }
        }

        return discrepancies;
    }
}
