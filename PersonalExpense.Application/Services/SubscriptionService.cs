using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly ITransactionService _transactionService;

    public SubscriptionService(ApplicationDbContext context, ITransactionService transactionService)
    {
        _context = context;
        _transactionService = transactionService;
    }

    public async Task<PagedResult<SubscriptionDto>> GetSubscriptionsAsync(Guid userId, SubscriptionFilterParams filter)
    {
        var query = _context.Subscriptions
            .Include(s => s.Account)
            .Include(s => s.Category)
            .Where(s => s.UserId == userId);

        if (filter.Status.HasValue)
        {
            query = query.Where(s => s.Status == filter.Status.Value);
        }

        if (filter.Frequency.HasValue)
        {
            query = query.Where(s => s.Frequency == filter.Frequency.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(s => s.Type == filter.Type.Value);
        }

        if (filter.DueBefore.HasValue)
        {
            query = query.Where(s => s.NextDueDate <= filter.DueBefore.Value);
        }

        if (filter.DueAfter.HasValue)
        {
            query = query.Where(s => s.NextDueDate >= filter.DueAfter.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(s => s.NextDueDate)
            .ThenBy(s => s.Name)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<SubscriptionDto>
        {
            Items = items.Select(s => MapToDto(s)).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid id, Guid userId)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Account)
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        return subscription != null ? MapToDto(subscription) : null;
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(SubscriptionCreateDto dto, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);

        if (account == null)
        {
            throw new BadRequestException("Account not found");
        }

        if (dto.CategoryId.HasValue)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value && c.UserId == userId);

            if (category == null)
            {
                throw new BadRequestException("Category not found");
            }
        }

        if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
        {
            throw new BadRequestException("End date cannot be earlier than start date");
        }

        var nextDueDate = CalculateNextDueDate(dto.StartDate, dto.Frequency, DateTime.UtcNow);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Amount = dto.Amount,
            Type = dto.Type,
            Frequency = dto.Frequency,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            NextDueDate = nextDueDate,
            LastPaidDate = null,
            Status = SubscriptionStatus.Active,
            Description = dto.Description,
            AccountId = dto.AccountId,
            CategoryId = dto.CategoryId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return await GetSubscriptionByIdAsync(subscription.Id, userId)
            ?? throw new NotFoundException(nameof(Subscription), subscription.Id);
    }

    public async Task<SubscriptionDto> UpdateSubscriptionAsync(Guid id, SubscriptionUpdateDto dto, Guid userId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subscription == null)
        {
            throw new NotFoundException(nameof(Subscription), id);
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);

        if (account == null)
        {
            throw new BadRequestException("Account not found");
        }

        if (dto.CategoryId.HasValue)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value && c.UserId == userId);

            if (category == null)
            {
                throw new BadRequestException("Category not found");
            }
        }

        if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
        {
            throw new BadRequestException("End date cannot be earlier than start date");
        }

        var nextDueDate = CalculateNextDueDate(dto.StartDate, dto.Frequency, DateTime.UtcNow);

        subscription.Name = dto.Name;
        subscription.Amount = dto.Amount;
        subscription.Type = dto.Type;
        subscription.Frequency = dto.Frequency;
        subscription.StartDate = dto.StartDate;
        subscription.EndDate = dto.EndDate;
        subscription.NextDueDate = nextDueDate;
        subscription.Status = dto.Status;
        subscription.Description = dto.Description;
        subscription.AccountId = dto.AccountId;
        subscription.CategoryId = dto.CategoryId;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetSubscriptionByIdAsync(subscription.Id, userId)
            ?? throw new NotFoundException(nameof(Subscription), subscription.Id);
    }

    public async Task DeleteSubscriptionAsync(Guid id, Guid userId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subscription == null)
        {
            throw new NotFoundException(nameof(Subscription), id);
        }

        _context.Subscriptions.Remove(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task<TransactionDto> RecordSubscriptionPaymentAsync(Guid subscriptionId, RecordSubscriptionPaymentDto dto, Guid userId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

        if (subscription == null)
        {
            throw new NotFoundException(nameof(Subscription), subscriptionId);
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            throw new BadRequestException("Cannot record payment for inactive subscription");
        }

        var transactionDto = new TransactionCreateDto(
            Type: subscription.Type,
            Amount: subscription.Amount,
            TransactionDate: dto.PaymentDate,
            Description: dto.Description ?? subscription.Name,
            AttachmentUrl: dto.AttachmentUrl,
            AccountId: subscription.AccountId,
            CategoryId: subscription.CategoryId,
            TransferToAccountId: null
        );

        var transaction = await _transactionService.CreateTransactionAsync(transactionDto, userId);

        var newTransaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        if (newTransaction != null)
        {
            newTransaction.SubscriptionId = subscriptionId;
        }

        subscription.LastPaidDate = dto.PaymentDate;
        subscription.NextDueDate = CalculateNextDueDate(subscription.NextDueDate, subscription.Frequency);
        subscription.UpdatedAt = DateTime.UtcNow;

        if (subscription.EndDate.HasValue && subscription.NextDueDate > subscription.EndDate.Value)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
        }

        await _context.SaveChangesAsync();

        return transaction;
    }

    public async Task<List<SubscriptionReminderDto>> GetUpcomingRemindersAsync(Guid userId, int daysInAdvance = 3)
    {
        var today = DateTime.UtcNow.Date;
        var reminderDate = today.AddDays(daysInAdvance);

        var subscriptions = await _context.Subscriptions
            .Include(s => s.Account)
            .Where(s => s.UserId == userId
                && s.Status == SubscriptionStatus.Active
                && s.NextDueDate.Date >= today
                && s.NextDueDate.Date <= reminderDate)
            .OrderBy(s => s.NextDueDate)
            .ToListAsync();

        return subscriptions.Select(s => new SubscriptionReminderDto(
            SubscriptionId: s.Id,
            SubscriptionName: s.Name,
            Amount: s.Amount,
            Type: s.Type,
            NextDueDate: s.NextDueDate,
            DaysUntilDue: (s.NextDueDate.Date - today).Days,
            AccountId: s.AccountId,
            AccountName: s.Account?.Name
        )).ToList();
    }

    public Task GenerateUpcomingTransactionsAsync(Guid userId)
    {
        return Task.CompletedTask;
    }

    private static DateTime CalculateNextDueDate(DateTime fromDate, SubscriptionFrequency frequency, DateTime? referenceDate = null)
    {
        var reference = referenceDate ?? DateTime.UtcNow;
        var nextDate = fromDate;

        while (nextDate <= reference)
        {
            nextDate = frequency switch
            {
                SubscriptionFrequency.Daily => nextDate.AddDays(1),
                SubscriptionFrequency.Weekly => nextDate.AddDays(7),
                SubscriptionFrequency.Monthly => nextDate.AddMonths(1),
                SubscriptionFrequency.Yearly => nextDate.AddYears(1),
                _ => nextDate.AddDays(1)
            };
        }

        return nextDate;
    }

    private static SubscriptionDto MapToDto(Subscription subscription)
    {
        var daysUntilDue = (subscription.NextDueDate.Date - DateTime.UtcNow.Date).Days;

        return new SubscriptionDto(
            Id: subscription.Id,
            Name: subscription.Name,
            Amount: subscription.Amount,
            Type: subscription.Type,
            Frequency: subscription.Frequency,
            StartDate: subscription.StartDate,
            EndDate: subscription.EndDate,
            NextDueDate: subscription.NextDueDate,
            LastPaidDate: subscription.LastPaidDate,
            Status: subscription.Status,
            Description: subscription.Description,
            AccountId: subscription.AccountId,
            AccountName: subscription.Account?.Name,
            CategoryId: subscription.CategoryId,
            CategoryName: subscription.Category?.Name,
            CreatedAt: subscription.CreatedAt,
            UpdatedAt: subscription.UpdatedAt,
            DaysUntilDue: daysUntilDue
        );
    }
}
