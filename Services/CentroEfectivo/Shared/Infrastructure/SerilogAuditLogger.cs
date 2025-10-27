using Microsoft.Extensions.Logging;
using VCashApp.Services.CentroEfectivo.Shared.Domain;

namespace VCashApp.Services.CentroEfectivo.Shared.Infrastructure
{
    public sealed class SerilogAuditLogger : IAuditLogger
    {
        private readonly ILogger<SerilogAuditLogger> _logger;
        public SerilogAuditLogger(ILogger<SerilogAuditLogger> logger) => _logger = logger;

        public void Info(string code, string message, string result, string entity, string entityId, string? extra = null) =>
            _logger.LogInformation("AUDIT {Code} {Message} {Result} {Entity} {EntityId} {Extra}", code, message, result, entity, entityId, extra ?? "");

        public void Warn(string code, string message, string result, string entity, string entityId, string? extra = null) =>
            _logger.LogWarning("AUDIT {Code} {Message} {Result} {Entity} {EntityId} {Extra}", code, message, result, entity, entityId, extra ?? "");

        public void Error(string code, string message, string result, string entity, string entityId, string? extra = null) =>
            _logger.LogError("AUDIT {Code} {Message} {Result} {Entity} {EntityId} {Extra}", code, message, result, entity, entityId, extra ?? "");
    }
}