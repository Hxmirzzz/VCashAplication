// VCashApp.Filters/RequiredPermissionFilter.cs

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
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


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
        private readonly AppDbContext _dbContext;

        public RequiredPermissionFilter(
            PermissionType permissionType,
            string codVista,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext)
        {
            _permissionType = permissionType;
            _codVista = codVista;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            string actionName = context.ActionDescriptor.DisplayName ?? "UnknownAction";
            string ipAddress = context.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            string userName = user.Identity?.Name ?? "NoAutenticado";

            // Usamos LogContext.PushProperty para añadir propiedades que Serilog capturará.
            // Los nombres aquí son los que el sink de MSSQLServer buscará en AdditionalColumns.
            // Pushing las propiedades generales que tus controladores usan.
            using (LogContext.PushProperty("IP", ipAddress))
            using (LogContext.PushProperty("Usuario", userName))
            using (LogContext.PushProperty("Accion", actionName))
            {
                // 1. Verificar autenticación
                if (!user.Identity.IsAuthenticated)
                {
                    Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: {Resultado} |",
                        userName, ipAddress, "Filtro de Autorización", "No autenticado. Redirigiendo a Login.");
                    context.Result = new RedirectToPageResult("/Account/Login", new { area = "Identity" });
                    return;
                }

                // 2. Obtener el ApplicationUser completo
                var applicationUser = await _userManager.GetUserAsync(user);
                if (applicationUser == null)
                {
                    Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: {Resultado} |",
                        userName, ipAddress, "Filtro de Autorización", "Acceso denegado (usuario Identity no encontrado).");
                    context.Result = new ForbidResult();
                    return;
                }

                // 3. Verificar si es Administrador (rol "Admin" tiene acceso total)
                if (await _userManager.IsInRoleAsync(applicationUser, "Admin"))
                {
                    Log.Information("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: {Resultado} |",
                        applicationUser.UserName, ipAddress, "Filtro de Autorización", "Acceso concedido (es Admin).");
                    return; // Los administradores tienen acceso total
                }

                // *** INICIO DE LA LÓGICA DE PERMISOS DATA-DRIVEN ***
                bool hasPermission = false;
                var userRoles = await _userManager.GetRolesAsync(applicationUser); // Obtener TODOS los nombres de roles del usuario

                // Log con nombres de propiedades estandarizados
                Log.Information("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Roles de usuario para permiso: {RolesUsuario}.",
                    applicationUser.UserName, ipAddress, "Filtro de Autorización", string.Join(", ", userRoles));

                foreach (var roleName in userRoles)
                {
                    var roleIdFromDb = await _dbContext.Roles.AsNoTracking()
                                                             .Where(r => r.Name == roleName)
                                                             .Select(r => r.Id)
                                                             .FirstOrDefaultAsync();

                    if (string.IsNullOrEmpty(roleIdFromDb))
                    {
                        Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | ID de rol '{RolNombre}' no encontrado en la base de datos para PermisosPerfil.",
                            applicationUser.UserName, ipAddress, "Filtro de Autorización", roleName);
                        continue;
                    }

                    var permisosDelPerfil = await _dbContext.PermisosPerfil.AsNoTracking()
                        .Where(p => p.CodPerfilId == roleIdFromDb && p.CodVista == _codVista)
                        .FirstOrDefaultAsync();

                    if (permisosDelPerfil != null)
                    {
                        // Log con nombres de propiedades estandarizados
                        Log.Information("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Permiso de perfil encontrado para RolId: {IdRolDB}, CodVista: {CodigoVista}. PuedeVer: {PuedeVer}, PuedeCrear: {PuedeCrear}, PuedeEditar: {PuedeEditar}.",
                            applicationUser.UserName, ipAddress, "Filtro de Autorización", roleIdFromDb, _codVista, permisosDelPerfil.PuedeVer, permisosDelPerfil.PuedeCrear, permisosDelPerfil.PuedeEditar);

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
                        Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | No se encontró permiso de perfil en PermisosPerfil para RolId: {IdRolDB} y CodVista: {CodigoVista} con el permiso requerido.",
                            applicationUser.UserName, ipAddress, "Filtro de Autorización", roleIdFromDb, _codVista);
                    }
                }

                if (!hasPermission)
                {
                    Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: {Resultado} | PermisoRequerido: {TipoPermiso}. Vista: '{CodigoVista}'. Roles del usuario: {RolesUsuario}.",
                        applicationUser.UserName, ipAddress, "Filtro de Autorización", "Acceso denegado", _permissionType.ToString(), _codVista, string.Join(", ", userRoles));
                    context.Result = new ForbidResult();
                    return;
                }

                Log.Information("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: {Resultado} | PermisoConcedido: {TipoPermiso}. Vista: '{CodigoVista}'. Roles del usuario: {RolesUsuario}.",
                    applicationUser.UserName, ipAddress, "Filtro de Autorización", "Acceso concedido", _permissionType.ToString(), _codVista, string.Join(", ", userRoles));
            }
        }
    }
}