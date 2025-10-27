using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VCashApp.Models;
using VCashApp.Infrastructure.Branches;
using static VCashApp.Infrastructure.Branches.BranchClaimTypes;
using VCashApp.Data;

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
            UserManager<ApplicationUser> um,
            AppDbContext db)
        {
            var path = http.Request.Path.Value?.ToLowerInvariant() ?? "";
            if (path.StartsWith("/identity") ||
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
                var rawActive = http.User.Claims.FirstOrDefault(c => c.Type == ActiveBranch)?.Value;

                if (string.Equals(rawActive, AllBranches, StringComparison.Ordinal))
                {
                    var appUser = await um.GetUserAsync(http.User);
                    if (appUser != null)
                    {
                        var ids = await db.UserClaims
                            .Where(uc => uc.UserId == appUser.Id && uc.ClaimType == AssignedBranch)
                            .Select(uc => uc.ClaimValue)
                            .ToListAsync();

                        var parsed = ids.Select(v => int.TryParse(v, out var n) ? (int?)n : null)
                            .Where(n => n.HasValue)
                            .Select(n => n!.Value)
                            .Distinct()
                            .ToList();

                        context.SetAllBranches(parsed);
                    }

                    await _next(http);
                    return;
                }

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
