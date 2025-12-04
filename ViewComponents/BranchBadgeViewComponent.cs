using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Models;
using System.Security.Claims;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.ViewComponents
{
    public class BranchBadgeViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _um;
        private readonly AppDbContext _db;

        public BranchBadgeViewComponent(UserManager<ApplicationUser> um, AppDbContext db)
        {
            _um = um;
            _db = db;
        }

        public class BranchOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class Vm
        {
            public string Display { get; set; } = "-";
            public int? ActiveBranchId { get; set; }
            public bool AllBranches { get; set; }
            public List<BranchOption> Options { get; set; } = new();
            public string? ReturnUrl { get; set; }
        }

        public async Task<IViewComponentResult> InvokeAsync(string? returnUrl = null)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return View(new Vm());
            }

            var user = await _um.GetUserAsync((ClaimsPrincipal)User);
            if (user == null)
                return View(new Vm());

            var active = ((ClaimsPrincipal)User).Claims
                .FirstOrDefault(c => c.Type == BranchClaimTypes.ActiveBranch)?.Value;

            var isAll = string.Equals(active, BranchClaimTypes.AllBranches, StringComparison.Ordinal);
            int? activeId = null;
            if (!isAll && int.TryParse(active, out var n)) activeId = n;

            var ids = await _db.UserClaims
                .Where(uc => uc.UserId == user.Id && uc.ClaimType == BranchClaimTypes.AssignedBranch)
                .Select(uc => uc.ClaimValue)
                .ToListAsync();

            var ints = ids
                .Select(v => int.TryParse(v, out var m) ? (int?)m : null)
                .Where(m => m.HasValue)
                .Select(m => m!.Value)
                .Distinct()
                .ToList();

            var options = await _db.AdmSucursales
                .Where(s => ints.Contains(s.CodSucursal))
                .OrderBy(s => s.NombreSucursal)
                .Select(s => new BranchOption { Id = s.CodSucursal, Name = s.NombreSucursal })
                .ToListAsync();

            var display = isAll
                ? "Todas"
                : activeId.HasValue
                    ? options.FirstOrDefault(o => o.Id == activeId)?.Name ?? "-"
                    : "-";

            return View(new Vm
            {
                Display = display,
                ActiveBranchId = activeId,
                AllBranches = isAll,
                Options = options,
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                    ? HttpContext.Request.Path + HttpContext.Request.QueryString
                    : returnUrl
            });
        }
    }
}
