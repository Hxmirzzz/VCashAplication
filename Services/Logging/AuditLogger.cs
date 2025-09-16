using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace VCashApp.Services.Logging
{
    public interface IAuditLogger
    {
        void Info(string action, string? detailMessage = null, string? result = null,
                  string? entityType = null, string? entityId = null,
                  string? urlId = null, string? modelId = null,
                  IReadOnlyDictionary<string, object>? extra = null);

        void Warn(string action, string? detailMessage = null, string? result = null,
                  string? entityType = null, string? entityId = null);

        void Error(string action, Exception ex, string? detailMessage = null,
                   string? entityType = null, string? entityId = null);
    }

    /// <summary>
    /// Emite eventos con la marca IsAudit=true y propiedades uniformes para AppLogs.
    /// </summary>
    public sealed class AuditLogger : IAuditLogger
    {
        private readonly ILogger<AuditLogger> _log;
        private readonly IHttpContextAccessor _http;

        public AuditLogger(ILogger<AuditLogger> log, IHttpContextAccessor http)
        {
            _log = log;
            _http = http;
        }

        private (string? user, string? ip, string? path) Context()
        {
            var ctx = _http.HttpContext;
            var user = ctx?.User?.Identity?.Name ?? "anonymous";
            var ip = ctx?.Connection?.RemoteIpAddress?.ToString();
            var path = ctx?.Request?.Path.Value;
            return (user, ip, path);
        }

        public void Info(string action, string? detailMessage = null, string? result = null,
                         string? entityType = null, string? entityId = null,
                         string? urlId = null, string? modelId = null,
                         IReadOnlyDictionary<string, object>? extra = null)
        {
            var (user, ip, path) = Context();

            using (_log.BeginScope(new Dictionary<string, object?>
            {
                ["IsAudit"] = true,
                ["Action"] = action,
                ["Username"] = user,
                ["IpAddress"] = ip,
                ["UrlPath"] = path,
                ["DetailMessage"] = detailMessage,
                ["Result"] = result,
                ["EntityType"] = entityType,
                ["EntityId"] = entityId,
                ["UrlId"] = urlId,
                ["ModelId"] = modelId
            }))
            {
                if (extra is not null)
                {
                    foreach (var kv in extra)
                        using (_log.BeginScope(new[] { new KeyValuePair<string, object?>(kv.Key, kv.Value) })) { }
                }

                _log.LogInformation("AUDIT {Action} {EntityType}#{EntityId} => {Result}", action, entityType, entityId, result);
            }
        }

        public void Warn(string action, string? detailMessage = null, string? result = null,
                         string? entityType = null, string? entityId = null)
        {
            var (user, ip, path) = Context();
            using (_log.BeginScope(new Dictionary<string, object?>
            {
                ["IsAudit"] = true,
                ["Action"] = action,
                ["Username"] = user,
                ["IpAddress"] = ip,
                ["UrlPath"] = path,
                ["DetailMessage"] = detailMessage,
                ["Result"] = result,
                ["EntityType"] = entityType,
                ["EntityId"] = entityId
            }))
            {
                _log.LogWarning("AUDIT-WARN {Action} {EntityType}#{EntityId} => {Result}", action, entityType, entityId, result);
            }
        }

        public void Error(string action, Exception ex, string? detailMessage = null,
                          string? entityType = null, string? entityId = null)
        {
            var (user, ip, path) = Context();
            using (_log.BeginScope(new Dictionary<string, object?>
            {
                ["IsAudit"] = true,
                ["Action"] = action,
                ["Username"] = user,
                ["IpAddress"] = ip,
                ["UrlPath"] = path,
                ["DetailMessage"] = detailMessage,
                ["EntityType"] = entityType,
                ["EntityId"] = entityId
            }))
            {
                _log.LogError(ex, "AUDIT-ERROR {Action} {EntityType}#{EntityId}", action, entityType, entityId);
            }
        }
    }
}
