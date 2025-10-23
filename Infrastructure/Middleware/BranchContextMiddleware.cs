using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Identity;
using VCashApp.Models;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Infrastructure.Middleware
{
    public sealed class BranchContextMiddleware
    {
        private readonly RequestDelegate _next;
        public BranchContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(
            HttpContext http,
            IBranchContext context,
            IBranchResolver resolver,
            UserManager<ApplicationUser> um)
        {
            var path = http.Request.Path.Value?.ToLowerInvariant() ?? "";
            if (path.StartsWith("/Identity") ||
                path.StartsWith("/account/selectbranch") ||
                path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/assets") ||
                path.StartsWith("/lib") || path.StartsWith("/swagger") || path.StartsWith("/health") ||
                path == "/")
            {
                await _next(http);
                return;
            }

            if (http.User?.Identity?.IsAuthenticated == true)
            {
                var active = await resolver.ResolveAsync(http.User);
                if (active.HasValue)
                {
                    context.SetBranch(active.Value);
                    await _next(http);
                    return;
                }

                bool requiresBranch =
                    path.StartsWith("/cef") || path.StartsWith("/service") ||
                    path.StartsWith("/rutasdiarias") || path.StartsWith("/tesoreria") ||
                    path.StartsWith("/cgs") || path.StartsWith("/adm") ||
                    path.StartsWith("/user");

                if (requiresBranch)
                {
                    http.Response.Redirect("/Account/SelectBranch?returnUrl=" + Uri.EscapeDataString(http.Request.Path + http.Request.QueryString));
                    return;
                }
            }

            await _next(http);
        }
    }
}
