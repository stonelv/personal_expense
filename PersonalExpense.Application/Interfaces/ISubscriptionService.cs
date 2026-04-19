using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface ISubscriptionService
{
    Task<PagedResult<SubscriptionDto>> GetSubscriptionsAsync(Guid userId, SubscriptionFilterParams filter);
    Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid id, Guid userId);
    Task<SubscriptionDto> CreateSubscriptionAsync(SubscriptionCreateDto dto, Guid userId);
    Task<SubscriptionDto> UpdateSubscriptionAsync(Guid id, SubscriptionUpdateDto dto, Guid userId);
    Task DeleteSubscriptionAsync(Guid id, Guid userId);
    Task<TransactionDto> RecordSubscriptionPaymentAsync(Guid subscriptionId, RecordSubscriptionPaymentDto dto, Guid userId);
    Task<List<SubscriptionReminderDto>> GetUpcomingRemindersAsync(Guid userId, int daysInAdvance = 3);
    Task<List<TransactionDto>> GenerateUpcomingTransactionsAsync(Guid userId);
}
