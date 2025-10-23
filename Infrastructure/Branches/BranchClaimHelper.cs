using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using VCashApp.Models;
using static VCashApp.Infrastructure.Branches.BranchClaimTypes;

namespace VCashApp.Infrastructure.Branches
{
    public static class BranchClaimHelper
    {
        public static async Task SetActiveBranchAsync(
            HttpContext http,
            SignInManager<ApplicationUser> sm,
            ApplicationUser user,
            int branchId)
        {
            var claims = await sm.UserManager.GetClaimsAsync(user);

            var existing = claims.FirstOrDefault(c => c.Type == ActiveBranch);
            if (existing != null)
                await sm.UserManager.RemoveClaimAsync(user, existing);

            await sm.UserManager.AddClaimAsync(user, new Claim(BranchClaimTypes.ActiveBranch, branchId.ToString()));
            await sm.RefreshSignInAsync(user);
        }
    }
}