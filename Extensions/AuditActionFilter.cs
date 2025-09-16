using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
using VCashApp.Services.Logging;

namespace VCashApp.Extentions
{
    public sealed class AuditActionFilter : IAsyncActionFilter, IAsyncExceptionFilter
    {
        private readonly IAuditLogger _audit;
        public AuditActionFilter(IAuditLogger audit) => _audit = audit;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();

            // Si hubo excepción sin manejar, la registra OnException
            if (executed.Exception != null && !executed.ExceptionHandled) return;

            var (controller, action) = GetNames(context);
            var entityId = ResolveId(context);
            var result = executed.Result switch
            {
                ObjectResult o => (o.StatusCode ?? 200).ToString(),
                StatusCodeResult sc => sc.StatusCode.ToString(),
                RedirectToActionResult => "302 Redirect",
                RedirectResult => "302 Redirect",
                ViewResult => "200 View",
                _ => "OK"
            };

            _audit.Info(
                action: $"{controller}.{action}",
                detailMessage: "Action executed",
                result: result,
                entityType: controller,  // simple y suficiente
                entityId: entityId
            );
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.ExceptionHandled) return Task.CompletedTask;

            var (controller, action) = GetNames(context);
            var entityId = ResolveId(context);

            _audit.Error(
                action: $"{controller}.{action}",
                ex: context.Exception,
                detailMessage: "Unhandled exception",
                entityType: controller,
                entityId: entityId
            );

            return Task.CompletedTask;
        }

        // Helpers mínimos
        private static (string controller, string action) GetNames(FilterContext ctx)
        {
            var cad = ctx.ActionDescriptor as ControllerActionDescriptor;
            var controller = cad?.ControllerName ?? (ctx.RouteData.Values["controller"]?.ToString() ?? "Unknown");
            var action = cad?.ActionName ?? (ctx.RouteData.Values["action"]?.ToString() ?? "Unknown");
            return (controller, action);
        }

        private static string? ResolveId(FilterContext ctx)
        {
            if (ctx.RouteData.Values.TryGetValue("id", out var idv)) return idv?.ToString();
            var firstId = ctx.RouteData.Values.FirstOrDefault(kv => kv.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase)).Value;
            return firstId?.ToString();
        }
    }
}