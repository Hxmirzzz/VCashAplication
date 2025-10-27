using VCashApp.Enums;

namespace VCashApp.Services.CentroEfectivo.Collection.Domain
{
    /// <summary>Reglas simples de negocio (precondiciones de creación/cierre/aprobación).</summary>
    public sealed class CountingPolicy : ICountingPolicy
    {
        public bool CanCreate(string soId, string currency, decimal declaredTotalValue)
        {
            if (string.IsNullOrWhiteSpace(soId)) return false;
            if (string.IsNullOrWhiteSpace(currency)) return false;
            if (declaredTotalValue < 0) return false;
            return true;
        }

        public bool CanFinalize(dynamic tx)
        {
            if (tx == null) return false;
            var counted = (decimal)(tx.TotalCountedValue ?? 0m);
            return counted >= 0m && string.Equals((string?)tx.TransactionStatus, nameof(CefTransactionStatusEnum.Conteo), StringComparison.OrdinalIgnoreCase);
        }

        public bool CanApprove(dynamic tx)
        {
            if (tx == null) return false;
            return string.Equals((string?)tx.TransactionStatus, nameof(CefTransactionStatusEnum.PendienteRevision), StringComparison.OrdinalIgnoreCase);
        }
    }
}
