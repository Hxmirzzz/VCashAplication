using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Services.CentroEfectivo.Provision.Domain;

namespace VCashApp.Services.CentroEfectivo.Shared.Domain
{
    public interface ICefContainerRepository
    {
        Task SaveContainersAndDetailsAsync(
            int txId,
            IReadOnlyList<CefContainerProcessingViewModel> containers,
            string userId,
            IAllowedValueTypesPolicy allowed);

        Task<CefContainer?> GetWithDetailAsync(int containerId);
        Task<List<CefContainer>> GetByTransactionIdAsync(int txId);

        Task<bool> DeleteAsync(int txId, int containerId);

        Task<(bool sobres, bool documentos, bool cheques)> GetPointCapsAsync(string serviceOrderId);

        Task<decimal> SumCountedAsync(int txId);

        // Totales (la misma lógica de TxTotals)
        Task<(decimal DeclaredCash, decimal BillHigh, decimal BillLow, decimal CoinTotal,
              decimal CheckTotal, decimal DocTotal)> GetTotalsAsync(int txId);
    }
}