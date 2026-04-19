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
    private const decimal Tolerance = 0.01m;

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

        var expectedBalance = CalculateExpectedBalance(transactions, accountId);

        var actualBalance = account.Balance;
        var discrepancy = actualBalance - expectedBalance;

        if (Math.Abs(discrepancy) > Tolerance)
        {
            discrepancies.Add(new DiscrepancyItemDto(
                Type: "BalanceMismatch",
                Description: $"账户余额与交易记录不一致。预期：{expectedBalance:F2}，实际：{actualBalance:F2}",
                Expected: expectedBalance,
                Actual: actualBalance,
                Difference: discrepancy
            ));
        }

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
                    Description: $"交易 {trans.Id} 缺少关联交易",
                    Expected: 1,
                    Actual: 0,
                    Difference: -1
                ));
            }
            else if (Math.Abs(related.Amount - trans.Amount) > Tolerance)
            {
                discrepancies.Add(new DiscrepancyItemDto(
                    Type: "AmountMismatch",
                    Description: $"转账金额不匹配：交易 {trans.Id} ({trans.Amount:F2}) 和 {related.Id} ({related.Amount:F2})",
                    Expected: trans.Amount,
                    Actual: related.Amount,
                    Difference: related.Amount - trans.Amount
                ));
            }
        }

        var isBalanced = discrepancies.Count == 0;

        return new ReconciliationResultDto(
            AccountId: account.Id,
            AccountName: account.Name,
            AccountType: account.Type,
            IsBalanced: isBalanced,
            ExpectedBalance: expectedBalance,
            ActualBalance: actualBalance,
            Discrepancy: discrepancy,
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
                    Description: $"交易 {referencing.Id} 的关联交易 {transferId} 不存在",
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
                    Math.Abs(t.Amount - transfer.Amount) < Tolerance);

                if (counterPart == null)
                {
                    discrepancies.Add(new DiscrepancyItemDto(
                        Type: "UnpairedTransfer",
                        Description: $"传统转账 {transfer.Id} 没有对应的配对记录",
                        Expected: 1,
                        Actual: 0,
                        Difference: -1
                    ));
                }
            }
        }

        return discrepancies;
    }

    private decimal CalculateExpectedBalance(List<Transaction> transactions, Guid accountId)
    {
        var expectedBalance = 0m;

        foreach (var trans in transactions)
        {
            switch (trans.Type)
            {
                case TransactionType.Income:
                    expectedBalance += trans.Amount;
                    break;
                case TransactionType.Expense:
                    expectedBalance -= trans.Amount;
                    break;
                case TransactionType.Transfer:
                    if (trans.TransferToAccountId == accountId)
                    {
                        expectedBalance += trans.Amount;
                    }
                    else
                    {
                        expectedBalance -= trans.Amount;
                    }
                    break;
            }
        }

        return expectedBalance;
    }
}
