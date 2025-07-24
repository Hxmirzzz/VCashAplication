using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using VCashApp.Models;
using VCashApp.Data;
using System.Security.Claims;
using Serilog;
using VCashApp.Enums;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VCashApp.Filters;
using Microsoft.Extensions.Logging;

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
    }
}