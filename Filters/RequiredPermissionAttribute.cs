using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using VCashApp.Models;
using VCashApp.Models.Entities;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using Serilog;
using Serilog.Context;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Necesario para IdentityRole


namespace VCashApp.Filters
{
    public enum PermissionType
    {
        View,
        Create,
        Edit
    }

    public class RequiredPermissionAttribute : TypeFilterAttribute
    {
        public RequiredPermissionAttribute(PermissionType permissionType, string codVista)
            : base(typeof(RequiredPermissionFilter))
        {
            Arguments = new object[] { permissionType, codVista };
        }
    }

    public class RequiredPermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly PermissionType _permissionType;
        private readonly string _codVista;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _dbContext; // Inyecta AppDbContext

        public RequiredPermissionFilter(
            PermissionType permissionType,
            string codVista,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext) // Recibe AppDbContext
        {
            _permissionType = permissionType;
            _codVista = codVista;
            _userManager = userManager;
            _dbContext = dbContext; // Asigna AppDbContext
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            string actionName = context.ActionDescriptor.DisplayName ?? "UnknownAction";
            string ipAddress = context.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            string userName = user.Identity?.Name ?? "NoAutenticado";

            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                // 1. Verificar autenticación
                if (!user.Identity.IsAuthenticated)
                {
                    Log.Warning("| AuthFilter: {Action} | Usuario: {User} | IP: {Ip} | Resultado: No autenticado. Redirigiendo a Login.", actionName, userName, ipAddress);
                    context.Result = new RedirectToPageResult("/Account/Login", new { area = "Identity" });
                    return;
                }

                // 2. Obtener el ApplicationUser completo
                var applicationUser = await _userManager.GetUserAsync(user);
                if (applicationUser == null)
                {
                    Log.Warning("| AuthFilter: {Action} | Usuario: {User} | IP: {Ip} | Resultado: Acceso denegado (usuario de Identity no encontrado).", actionName, userName, ipAddress);
                    context.Result = new ForbidResult();
                    return;
                }

                // 3. Verificar si es Administrador (rol "Admin" tiene acceso total)
                if (await _userManager.IsInRoleAsync(applicationUser, "Admin"))
                {
                    Log.Information("| AuthFilter: {Action} | Usuario: {User} (Admin) | IP: {Ip} | Resultado: Acceso concedido (es Admin).", actionName, applicationUser.UserName, ipAddress);
                    return; // Los administradores tienen acceso total
                }

                // *** INICIO DE LA LÓGICA DE PERMISOS DATA-DRIVEN ***
                bool hasPermission = false;
                var userRoles = await _userManager.GetRolesAsync(applicationUser); // Obtener TODOS los nombres de roles del usuario

                // Log los roles que se encontraron para este usuario
                Log.Information("| AuthFilter: {Action} | Usuario: {User} | IP: {Ip} | Roles de usuario para permiso: {UserRoles}.", actionName, applicationUser.UserName, ipAddress, string.Join(", ", userRoles));

                foreach (var roleName in userRoles)
                {
                    var roleIdFromDb = await _dbContext.Roles.AsNoTracking()
                                                       .Where(r => r.Name == roleName)
                                                       .Select(r => r.Id)
                                                       .FirstOrDefaultAsync();

                    Log.Information("| AuthFilter: Buscando permiso para rol '{RoleName}' (Id: {RoleIdFromDb}) para vista '{CodVista}'", roleName, roleIdFromDb, _codVista);


                    if (string.IsNullOrEmpty(roleIdFromDb))
                    {
                        Log.Warning("| AuthFilter: ID de rol '{RoleName}' no encontrado en la base de datos (AspNetRoles) para PermisosPerfil.", roleName);
                        continue; // Si el ID del rol no se encuentra, pasar al siguiente rol
                    }

                    // Consultar PermisosPerfil: buscar una entrada para este roleId Y este CodVista
                    var permisosDelPerfil = await _dbContext.PermisosPerfil.AsNoTracking() // También AsNoTracking aquí
                        .Where(p => p.CodPerfilId == roleIdFromDb && p.CodVista == _codVista)
                        .FirstOrDefaultAsync();

                    if (permisosDelPerfil != null)
                    {
                        Log.Information("| AuthFilter: Permiso de perfil encontrado para RoleId: {RoleIdFromDb}, CodVista: {CodVista}. PuedeVer: {PuedeVer}, PuedeCrear: {PuedeCrear}, PuedeEditar: {PuedeEditar}.",
                            roleIdFromDb, _codVista, permisosDelPerfil.PuedeVer, permisosDelPerfil.PuedeCrear, permisosDelPerfil.PuedeEditar);

                        switch (_permissionType)
                        {
                            case PermissionType.View:
                                if (permisosDelPerfil.PuedeVer) hasPermission = true;
                                break;
                            case PermissionType.Create:
                                if (permisosDelPerfil.PuedeCrear) hasPermission = true;
                                break;
                            case PermissionType.Edit:
                                if (permisosDelPerfil.PuedeEditar) hasPermission = true;
                                break;
                        }
                        if (hasPermission) break;
                    }
                    else
                    {
                        Log.Warning("| AuthFilter: No se encontró permiso de perfil en PermisosPerfil para RoleId: {RoleIdFromDb} y CodVista: {CodVista} con el permiso requerido.", roleIdFromDb, _codVista);
                    }
                }

                if (!hasPermission)
                {
                    Log.Warning("| AuthFilter: {Action} | Usuario: {User} | IP: {Ip} | Resultado: Acceso denegado (Sin permiso '{PermissionType}' para vista '{CodVista}'). Roles del usuario: {UserRoles}.",
                        actionName, applicationUser.UserName, ipAddress, _permissionType, _codVista, string.Join(", ", userRoles));
                    context.Result = new ForbidResult();
                    return;
                }

                Log.Information("| AuthFilter: {Action} | Usuario: {User} | IP: {Ip} | Resultado: Acceso concedido. TipoPermiso: {PermissionType}. Vista: '{CodVista}'. Roles del usuario: {UserRoles}.",
                    actionName, applicationUser.UserName, ipAddress, _permissionType, _codVista, string.Join(", ", userRoles));
            }
        }
    }
}