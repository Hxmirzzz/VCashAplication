using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using VCashApp.Models;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;

namespace VCashApp.Infrastructure.Branches
{
    public interface IBranchResolver
    {
        Task<int?> ResolveAsync(ClaimsPrincipal user);
        Task<bool> UserOwnsBranchAsync(string userId, int branchId);
    }

    public sealed class BranchResolver : IBranchResolver
    {
        private readonly UserManager<ApplicationUser> _um;
        private readonly AppDbContext _db;

        public BranchResolver(UserManager<ApplicationUser> um, AppDbContext db)
        {
            _um = um;
            _db = db;
        }

        public async Task<int?> ResolveAsync(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var appUser = await _um.GetUserAsync(user);
            if (appUser == null)
                return null;

            var active = (await _um.GetClaimsAsync(appUser))
                .FirstOrDefault(c => c.Type == BranchClaimTypes.ActiveBranch)?.Value;
            if (int.TryParse(active, out var activeId))
                return activeId;

            var branchIds = await _db.UserClaims
                .Where(uc => uc.UserId == appUser.Id && uc.ClaimType == BranchClaimTypes.AssignedBranch)
                .Select(uc => uc.ClaimValue)
                .ToListAsync();

            var parsed = branchIds.Select(v => int.TryParse(v, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList();

            if (parsed.Count == 1)
                return parsed[0];
            return null;
        }

        public async Task<bool> UserOwnsBranchAsync(string userId, int branchId)
        {
            return await _db.UserClaims
                .AnyAsync(uc => uc.UserId == userId
                    && uc.ClaimType == BranchClaimTypes.AssignedBranch
                    && uc.ClaimValue == branchId.ToString());
        }
    }
}
