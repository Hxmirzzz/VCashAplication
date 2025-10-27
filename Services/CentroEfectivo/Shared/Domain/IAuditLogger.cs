namespace VCashApp.Services.CentroEfectivo.Shared.Domain
{
    /// <summary>Logger de auditoría simple (puedes apuntarlo a Serilog sinks).</summary>
    public interface IAuditLogger
    {
        void Info(string code, string message, string result, string entity, string entityId, string? extra = null);
        void Warn(string code, string message, string result, string entity, string entityId, string? extra = null);
        void Error(string code, string message, string result, string entity, string entityId, string? extra = null);
    }
}