using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Utils;

namespace VCashApp.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly AppDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected string IpAddressForLogging { get; private set; } = "Desconocida";

        public BaseController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Método auxiliar centralizado para obtener el ApplicationUser
        protected async Task<ApplicationUser?> GetCurrentApplicationUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        // Método auxiliar centralizado para obtener el primer rol como perfil
        protected async Task<string?> GetCurrentCodPerfilAsync(ApplicationUser user)
        {
            if (user == null) return null;
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }

        protected async Task<bool> HasPermisionForView(IList<string> userRoles, string codView, PermissionType permissionType)
        {
            if (userRoles.Contains("Admin"))
            {
                return true;
            }

            foreach (var roleNameInString in userRoles)
            {
                var roleIdFromDb = await _context.Roles.AsNoTracking() // Usar AsNoTracking para evitar caché
                                                       .Where(r => r.Name == roleNameInString)
                                                       .Select(r => r.Id)
                                                       .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(roleIdFromDb))
                {
                    continue;
                }

                var permisosDelPerfil = await _context.PermisosPerfil.AsNoTracking() // También AsNoTracking aquí
                    .Where(p => p.CodPerfilId == roleIdFromDb && p.CodVista == codView)
                    .FirstOrDefaultAsync();

                if (permisosDelPerfil != null)
                {
                    switch (permissionType)
                    {
                        case PermissionType.View:
                            if (permisosDelPerfil.PuedeVer)
                            {
                                return true;
                            }
                            break;
                        case PermissionType.Create:
                            if (permisosDelPerfil.PuedeCrear)
                            {
                                return true;
                            }
                            break;
                        case PermissionType.Edit:
                            if (permisosDelPerfil.PuedeEditar)
                            {
                                return true;
                            }
                            break;
                        /*case PermissionType.Delete: // Este caso es solo si tu PermisoPerfil tiene PuedeEliminar
                            // if (permisosDelPerfil.PuedeEliminar)
                            // {
                            //     Log.Information("DEPURACION PERMISOS (Base): Permiso de Eliminar concedido para rol {RoleNameInString}.", roleNameInString);
                            //     return true;
                            // }
                            break;*/
                    }
                }
            }
            return false;
        }

        // Método auxiliar centralizado para obtener sucursales permitidas
        protected async Task<List<int>> GetUserPermittedSucursalesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<int>();

            var sucursalClaims = await _userManager.GetClaimsAsync(user);
            return sucursalClaims.Where(c => c.Type == "SucursalId")
                                 .Select(c => int.Parse(c.Value))
                                 .ToList();
        }

        // Método auxiliar centralizado para SetCommonViewBagsBaseAsync
        protected async Task SetCommonViewBagsBaseAsync(ApplicationUser currentUser, string pageName)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            IpAddressForLogging = ipAddress;
            ViewBag.Ip = ipAddress;

            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            string? currentCodPerfil = (await _userManager.GetRolesAsync(currentUser)).FirstOrDefault();

            ViewBag.UserName = currentUser.UserName?.ToUpper();
            ViewBag.UnidadName = "N/A";
            ViewBag.PageName = pageName;
            ViewBag.SucursalName = "N/A";
            ViewBag.NombreCompleto = currentUser.NombreUsuario ?? currentUser.UserName ?? "N/A";
            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentCodPerfil = currentCodPerfil;
        }

        // NUEVA ACTUALIZACIÓN: MÉTODOS BASADOS EN ROLE ID (GUID)

        /// <summary>
        /// IDs (GUID) de roles del usuario actual.
        /// </summary>
        protected async Task<List<string>> GetUserRoleIdsAsync(string userId)
        {
            return await _context.UserRoles
                                 .Where(ur => ur.UserId == userId)
                                 .Select(ur => ur.RoleId)
                                 .ToListAsync();
        }

        /// <summary>
        /// ¿El usuario pertenece al rol (por GUID)?
        /// </summary>
        protected Task<bool> IsInRoleIdAsync(ApplicationUser user, string roleId)
        {
            return _context.UserRoles
                .AsNoTracking()
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == roleId);
        }

        /// <summary>
        /// Versión por GUID: evalúa permisos de vista según PermisosPerfil.CodPerfilId (GUID de rol).
        /// </summary>
        protected async Task<bool> HasPermisionForViewByRoleIds(IEnumerable<string> roleIds, string codView, PermissionType permissionType)
        {
            if (roleIds == null) return false;
            if (roleIds.Contains(RoleIds.Admin))
                return true;

            var q = _context.PermisosPerfil.AsNoTracking()
                .Where(p => roleIds.Contains(p.CodPerfilId) && p.CodVista == codView);

            return permissionType switch
            {
                PermissionType.View => await q.AnyAsync(p => p.PuedeVer),
                PermissionType.Create => await q.AnyAsync(p => p.PuedeCrear),
                PermissionType.Edit => await q.AnyAsync(p => p.PuedeEditar),
                _ => false
            };
        }

        protected async Task<CefCaps> GetCefCapsAsync(ApplicationUser user)
        {
            var roleIds = await GetUserRoleIdsAsync(user.Id);
            var roleNames = await _userManager.GetRolesAsync(user); // sólo para HasPermisionForView

            bool Has(string id) => roleIds.Contains(id);

            var canBills = Has(RoleIds.ContadorBilleteCEF) || Has(RoleIds.SupervisorCEF);
            var canCoins = Has(RoleIds.ContadorMonedaCEF) || Has(RoleIds.SupervisorCEF);

            var canIncCreateEdit =
                   Has(RoleIds.ContadorBilleteCEF)
                || Has(RoleIds.ContadorMonedaCEF)
                || await HasPermisionForView(roleNames, "CEF_COL", PermissionType.Edit)
                || await HasPermisionForView(roleNames, "CEF_REC", PermissionType.Edit);

            var canIncApprove =
                   Has(RoleIds.SupervisorCEF)
                || await HasPermisionForView(roleNames, "CEF_SUP", PermissionType.Edit)
                || await HasPermisionForView(roleNames, "CEF_DEL", PermissionType.Edit);

            var canFinalize =
                   Has(RoleIds.SupervisorCEF)
                || await HasPermisionForView(roleNames, "CEF_SUP", PermissionType.Edit)
                || await HasPermisionForView(roleNames, "CEF_DEL", PermissionType.Edit);

            return new CefCaps(canBills, canCoins, canIncCreateEdit, canIncApprove, canFinalize);
        }
    }
}