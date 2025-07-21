using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;

namespace VCashApp.Extentions
{
    public class LogIpMiddleware
    {
        private readonly RequestDelegate _next;

        public LogIpMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                await _next(context);
            }
        }
    }
}