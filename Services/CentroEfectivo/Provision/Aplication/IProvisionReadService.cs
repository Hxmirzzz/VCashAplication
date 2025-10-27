using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Models.ViewModels.CentroEfectivo.Provision;

namespace VCashApp.Services.CentroEfectivo.Provision.Application
{
    /// <summary>Consultas para vistas (queries).</summary>
    public interface IProvisionReadService
    {
        Task<CefProcessContainersPageViewModel?> GetProcessPageAsync(int txId);
        Task<CefTransactionDetailViewModel> GetDetailAsync(int txId);
        Task<ProvisionSummaryVm> GetSummaryAsync(int txId);
        Task<CefProvisionDeliveryViewModel?> GetDeliveryAsync(int txId, string? returnUrl);
    }
}