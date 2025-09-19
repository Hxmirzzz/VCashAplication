using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Threading.Tasks;

namespace VCashApp.Extentions
{
    public class LogIpMiddleware
    {
        private readonly RequestDelegate _next;
        public LogIpMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var username = (context.User?.Identity?.IsAuthenticated ?? false)
                ? (context.User?.Identity?.Name ?? "anon")
                : "anon";

            var ip = context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            var urlPath = context.Request?.Path.Value ?? "";
            var method = context.Request?.Method ?? "";
            var requestId = context.TraceIdentifier;

            using (LogContext.PushProperty("Username", username))
            using (LogContext.PushProperty("IpAddress", ip))
            using (LogContext.PushProperty("UrlPath", urlPath))
            using (LogContext.PushProperty("HttpMethod", method))
            using (LogContext.PushProperty("RequestId", requestId))
            {
                await _next(context);
            }
        }
    }
}