using System.Net.Http.Json;
using PersonalExpense.Web.DTOs;

namespace PersonalExpense.Web.Services;

public interface ISubscriptionService
{
    Task<PagedResult<SubscriptionDto>?> GetSubscriptionsAsync(
        SubscriptionStatus? status = null,
        SubscriptionFrequency? frequency = null,
        TransactionType? type = null,
        DateTime? dueBefore = null,
        DateTime? dueAfter = null,
        int pageNumber = 1,
        int pageSize = 20);

    Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid id);
    Task<SubscriptionDto?> CreateSubscriptionAsync(SubscriptionCreateDto dto);
    Task<bool> UpdateSubscriptionAsync(Guid id, SubscriptionUpdateDto dto);
    Task<bool> DeleteSubscriptionAsync(Guid id);
    Task<List<SubscriptionReminderDto>?> GetUpcomingRemindersAsync(int daysInAdvance = 3);
    Task<TransactionDto?> RecordPaymentAsync(Guid subscriptionId, RecordSubscriptionPaymentDto dto);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly HttpClient _httpClient;

    public SubscriptionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<SubscriptionDto>?> GetSubscriptionsAsync(
        SubscriptionStatus? status = null,
        SubscriptionFrequency? frequency = null,
        TransactionType? type = null,
        DateTime? dueBefore = null,
        DateTime? dueAfter = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (status.HasValue)
            query.Add($"status={(int)status}");
        if (frequency.HasValue)
            query.Add($"frequency={(int)frequency}");
        if (type.HasValue)
            query.Add($"type={(int)type}");
        if (dueBefore.HasValue)
            query.Add($"dueBefore={dueBefore.Value:yyyy-MM-dd}");
        if (dueAfter.HasValue)
            query.Add($"dueAfter={dueAfter.Value:yyyy-MM-dd}");

        var queryString = string.Join("&", query);
        return await _httpClient.GetFromJsonAsync<PagedResult<SubscriptionDto>>($"api/subscriptions?{queryString}");
    }

    public async Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<SubscriptionDto>($"api/subscriptions/{id}");
    }

    public async Task<SubscriptionDto?> CreateSubscriptionAsync(SubscriptionCreateDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/subscriptions", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        }
        return null;
    }

    public async Task<bool> UpdateSubscriptionAsync(Guid id, SubscriptionUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/subscriptions/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/subscriptions/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<SubscriptionReminderDto>?> GetUpcomingRemindersAsync(int daysInAdvance = 3)
    {
        return await _httpClient.GetFromJsonAsync<List<SubscriptionReminderDto>>($"api/subscriptions/reminders?daysInAdvance={daysInAdvance}");
    }

    public async Task<TransactionDto?> RecordPaymentAsync(Guid subscriptionId, RecordSubscriptionPaymentDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/subscriptions/{subscriptionId}/record-payment", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransactionDto>();
        }
        return null;
    }
}
