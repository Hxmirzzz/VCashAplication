using System.Threading.Tasks;

namespace VCashApp.Services.CentroEfectivo.Provision.Infrastructure
{
    /// <summary>Argumentos para crear transacción de provisión.</summary>
    public sealed class CefTransactionNewArgs
    {
        public string ServiceOrderId { get; set; } = default!;
        public string Currency { get; set; } = default!;
        public decimal DeclaredBill { get; set; }
        public decimal DeclaredCoin { get; set; }
        public string? Observations { get; set; }
        public string UserId { get; set; } = default!;
    }

    /// <summary>Acceso a transacciones CEF para provisión.</summary>
    public interface ICefTransactionRepository
    {
        Task<int> AddProvisionAsync(CefTransactionNewArgs args);
        Task<dynamic?> GetAsync(int txId); // usa tu entidad concreta si prefieres
        Task<dynamic?> GetWithTotalsAsync(int txId);
        Task UpdateAsync(dynamic entity);
        Task RecalculateTotalsAsync(int txId);
    }
}