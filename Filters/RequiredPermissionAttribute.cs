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

    /// <summary>
    /// Attribute para exigir permisos por vista(s). OR entre las vistas recibidas.
    /// </summary>
    public class RequiredPermissionAttribute : TypeFilterAttribute
    {
        public RequiredPermissionAttribute(PermissionType permissionType, string codVista)
            : base(typeof(RequiredPermissionFilter))
        {
            Arguments = new object[] { permissionType, new[] { codVista } };
        }
        public RequiredPermissionAttribute(PermissionType permissionType, params string[] codVistas)
            : base(typeof(RequiredPermissionFilter))
        {
            Arguments = new object[] { permissionType, codVistas ?? Array.Empty<string>() };
        }
    }

    /// <summary>
    /// Filtro de autorización que valida contra PermisosPerfil.
    /// - Admin: acceso total.
    /// - Para el resto: OR entre TODAS las combinaciones (rol del usuario × codVista).
    ///   Si cualquier combinación concede el permiso, se autoriza.
    /// </summary>
    public class RequiredPermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly PermissionType _permissionType;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _dbContext;
        private readonly string[] _codVistas;

        public RequiredPermissionFilter(
            PermissionType permissionType,
            string[] codVistas,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext)
        {
            _permissionType = permissionType;
            _codVistas = codVistas ?? Array.Empty<string>();
            _userManager = userManager;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Aplica la lógica de autorización basada en permisos.
        /// </summary>
        /// <param name="context">Contexto del filtro de autorización.</param>
        /// <returns>Una tarea que representa la operación asincrónica.</returns>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            string actionName = context.ActionDescriptor.DisplayName ?? "UnknownAction";
            string ipAddress = context.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            string userName = user.Identity?.Name ?? "NoAutenticado";

            using (LogContext.PushProperty("IpAddress", ipAddress))
            using (LogContext.PushProperty("Usuario", userName))
            using (LogContext.PushProperty("Accion", actionName))
            {
                // 1) Requiere autenticación
                if (!user.Identity?.IsAuthenticated ?? true)
                {
                    Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: No autenticado. Redirigiendo a Login.",
                        userName, ipAddress, "Filtro de Autorización");
                    context.Result = new RedirectToPageResult("/Account/Login", new { area = "Identity" });
                    return;
                }

                // 2) Cargar ApplicationUser
                var applicationUser = await _userManager.GetUserAsync(user);
                if (applicationUser == null)
                {
                    Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: Acceso denegado (usuario Identity no encontrado).",
                        userName, ipAddress, "Filtro de Autorización");
                    context.Result = new ForbidResult();
                    return;
                }

                // 3) Admin => acceso total
                if (await _userManager.IsInRoleAsync(applicationUser, "Admin"))
                {
                    Log.Information("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: Acceso concedido (es Admin).",
                        applicationUser.UserName, ipAddress, "Filtro de Autorización");
                    return;
                }

                // 4) Roles del usuario
                var userRoles = await _userManager.GetRolesAsync(applicationUser);
                Log.Information("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Roles: {RolesUsuario}",
                    applicationUser.UserName, ipAddress, "Filtro de Autorización", string.Join(", ", userRoles));

                if (!_codVistas.Any())
                {
                    // Si no se pasó ningún codVista, por seguridad negar
                    Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: Acceso denegado (sin codVistas).",
                        applicationUser.UserName, ipAddress, "Filtro de Autorización");
                    context.Result = new ForbidResult();
                    return;
                }

                // 5) OR global: si cualquier (rol × vista) concede el permiso, permitir
                bool hasPermission = false;

                foreach (var roleName in userRoles)
                {
                    // Busca id de rol por Name (si manejas NormalizedName en mayúsculas, puedes usar ToUpperInvariant)
                    var roleIdFromDb = await _dbContext.Roles.AsNoTracking()
                        .Where(r => r.Name == roleName || r.NormalizedName == roleName.ToUpper())
                        .Select(r => r.Id)
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrEmpty(roleIdFromDb))
                    {
                        Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | ID de rol '{RolNombre}' no encontrado en la base de datos.",
                            applicationUser.UserName, ipAddress, "Filtro de Autorización", roleName);
                        continue;
                    }

                    foreach (var codVista in _codVistas)
                    {
                        var permiso = await _dbContext.PermisosPerfil
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.CodPerfilId == roleIdFromDb && p.CodVista == codVista);

                        if (permiso == null)
                        {
                            Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | No se encontró permiso en PermisosPerfil para RolId: {IdRolDB} y CodVista: {CodigoVista}.",
                                applicationUser.UserName, ipAddress, "Filtro de Autorización", roleIdFromDb, codVista);
                            continue;
                        }

                        Log.Information("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Permiso hallado | RolId:{IdRolDB} CodVista:{CodigoVista} Ver:{PuedeVer} Crear:{PuedeCrear} Edit:{PuedeEditar}",
                            applicationUser.UserName, ipAddress, "Filtro de Autorización",
                            roleIdFromDb, codVista, permiso.PuedeVer, permiso.PuedeCrear, permiso.PuedeEditar);

                        hasPermission =
                            (_permissionType == PermissionType.View && permiso.PuedeVer) ||
                            (_permissionType == PermissionType.Create && permiso.PuedeCrear) ||
                            (_permissionType == PermissionType.Edit && permiso.PuedeEditar);

                        if (hasPermission)
                            break; // basta un match por rol×vista
                    }

                    if (hasPermission)
                        break; // basta un rol que conceda
                }

                // 6) Decisión final
                if (!hasPermission)
                {
                    Log.Warning("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: Acceso denegado | Permiso: {TipoPermiso}. Vistas requeridas (OR): {CodVistas} | Roles: {RolesUsuario}.",
                        applicationUser.UserName, ipAddress, "Filtro de Autorización",
                        _permissionType.ToString(),
                        string.Join(",", _codVistas),
                        string.Join(", ", userRoles));
                    context.Result = new ForbidResult();
                    return;
                }

                Log.Information("Usuario: {Usuario} | IP: {IP} | Acción: {Accion} | Resultado: Acceso concedido | Permiso: {TipoPermiso}. Vistas OK (OR): {CodVistas}.",
                    applicationUser.UserName, ipAddress, "Filtro de Autorización",
                    _permissionType.ToString(),
                    string.Join(",", _codVistas));
            }
        }
    }
}