using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Enums;


namespace VCashApp.Services.CentroEfectivo.Shared.Domain
{
    public interface ICefIncidentRepository
    {
        Task<CefIncident> RegisterAsync(CefIncidentViewModel vm, string reportedUserId);
        Task<bool> ResolveAsync(int incidentId, string newStatus);

        Task<List<CefIncident>> GetAsync(int? txId = null, int? containerId = null, int? valueDetailId = null);
        Task<CefIncident?> GetByIdAsync(int incidentId);
        Task<List<CefIncidentType>> GetTypesAsync();

        Task<decimal> SumApprovedEffectByTransactionAsync(int txId);
        Task<decimal> SumApprovedEffectByContainerAsync(int containerId);
        Task<bool> HasPendingByTransactionAsync(int txId);

        Task<bool> UpdateReportedAsync(int id, int? newTypeId, CefIncidentTypeCategoryEnum? newType,
            int? newDenominationId, int? newQuantity, decimal? newAmount, string? newDescription);

        Task<bool> DeleteReportedAsync(int id);
    }
}
