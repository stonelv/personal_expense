using PersonalExpense.Application.DTOs;

namespace PersonalExpense.Application.Interfaces;

public interface IReconciliationService
{
    Task<ReconciliationResultDto> ReconcileAccountAsync(Guid accountId, Guid userId);
    Task<List<ReconciliationResultDto>> ReconcileAllAccountsAsync(Guid userId);
    Task<List<DiscrepancyItemDto>> DetectTransferDiscrepanciesAsync(Guid userId);
}
