using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Models;
using VCashApp.Extensions;
using Serilog;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Controllers
{
    [Authorize]
    [Route("/Account")]
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _um;
        private readonly SignInManager<ApplicationUser> _sm;

        public AccountController(AppDbContext db, UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm)
        {
            _db = db;
            _um = um;
            _sm = sm;
        }

        [HttpGet("SelectBranch")]
        public async Task<IActionResult> SelectBranch(string? returnUrl = null)
        {
            var user = await _um.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var brancheIds = await _db.UserClaims
                .Where(c => c.UserId == user.Id && c.ClaimType == "SucursalId")
                .Select(c => c.ClaimValue).ToListAsync();

            var parsed = brancheIds.Select(v => int.TryParse(v, out var n) ? (int?)n : null)
                .Where(n => n.HasValue).Select(n => n!.Value).Distinct().ToList();

            var branches = await _db.AdmSucursales
                .Where(s => parsed.Contains(s.CodSucursal))
                .OrderBy(s => s.NombreSucursal)
                .Select(s => new BranchItem { Id = s.CodSucursal, Name = s.NombreSucursal })
                .ToListAsync();

            return View(new SelectBranchVm { Branches = branches, ReturnUrl = returnUrl });
        }

        [ValidateAntiForgeryToken]
        [HttpPost("SelectBranch")]
        public async Task<IActionResult> SelectBranchPost(
            [FromForm] int branchId, 
            [FromForm] bool rememberAsDefault, 
            [FromForm] string? returnUrl = null)
        {
            var user = await _um.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var ok = await new BranchResolver(_um, _db).UserOwnsBranchAsync(user.Id, branchId);
            if (!ok) return Forbid();

            await BranchClaimHelper.SetActiveBranchAsync(HttpContext, _sm, user, branchId);

            Log.Information("Usuario {User} seleccionó sucursal {BranchId}", user.UserName, branchId);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        public sealed record class BranchItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
        }

        public sealed class SelectBranchVm
        {
            public List<BranchItem> Branches { get; set; } = new();
            public string? ReturnUrl { get; set; }
        }
    }
}
