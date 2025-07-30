using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCashApp.Data;
using VCashApp.Models;
using VCashApp.Models.ViewModels;
using VCashApp.Services;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Services.DTOs;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de usuarios.
    /// Proporciona funcionalidades para listar, crear, editar y visualizar información de usuarios.
    /// </summary>
    [Authorize]
    [Route("/User")]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IExportService _exportService;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Constructor del controlador UserController.
        /// </summary>
        /// <param name="userService">Servicio para la lógica de negocio de usuarios.</param>
        /// <param name="exportService">Servicio para funcionalidades de exportación.</param>
        /// <param name="context">Contexto de la base de datos de la aplicación.</param>
        /// <param name="userManager">Administrador de usuarios para gestionar ApplicationUser.</param>
        /// <param name="logger">Servicio de logging para el controlador.</param>
        public UserController(
            IUserService userService,
            IExportService exportService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UserController> logger)
            : base(context, userManager)
        {
            _userService = userService;
            _exportService = exportService;
            _logger = logger;
        }

        // Método auxiliar para configurar ViewBags comunes para las vistas de usuario
        private async Task SetCommonViewBagsUserAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            var (roles, sucursales) = await _userService.GetDropdownListsAsync();
            ViewBag.RolesList = new SelectList(roles, "Value", "Text");
            ViewBag.AvailableBranchesList = new SelectList(sucursales, "Value", "Text");

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreatePermission = await HasPermisionForView(userRoles, "USER", PermissionType.Create);
            ViewBag.HasEditPermission = await HasPermisionForView(userRoles, "USER", PermissionType.Edit);
            ViewBag.HasViewPermission = await HasPermisionForView(userRoles, "USER", PermissionType.View);
        }

        /// <summary>
        /// Muestra una lista paginada y filtrada de usuarios.
        /// </summary>
        /// <remarks>
        /// Esta acción sirve la página principal de listado de usuarios y puede devolver una vista parcial para solicitudes AJAX.
        /// Requiere permiso 'View' para "USER".
        /// </remarks>
        /// <param name="page">Número de página actual (default 1).</param>
        /// <param name="pageSize">Número de usuarios por página (default 15).</param>
        /// <param name="search">Término de búsqueda (nombre de usuario, email).</param>
        /// <param name="selectedRoleFilter">Filtro por nombre de rol.</param>
        /// <param name="selectedBranchFilter">Filtro por ID de sucursal.</param>
        /// <returns>La vista con la tabla de usuarios paginada.</returns>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "USER")]
        public async Task<IActionResult> Index(
            int page = 1, int pageSize = 15,
            string? search = null, string? selectedRoleFilter = null, int? selectedBranchFilter = null)
        {   
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsUserAsync(currentUser, "Usuarios");

            var (users, totalCount) = await _userService.GetFilteredUsersAsync(
                search, selectedRoleFilter, selectedBranchFilter,
                page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalData = totalCount;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedRoleFilter = selectedRoleFilter;
            ViewBag.SelectedBranchFilter = selectedBranchFilter;

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Accion: {Accion} | Conteo: {Conteo} | Resultado: {Resultado}",
                currentUser.UserName,
                IpAddressForLogging,
                "Accediendo a la lista de Usuarios",
                users.Count(),
                "Acceso concedido");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_UserTablePartial", users);
            }

            return View(users);
        }

        /// <summary>
        /// Muestra el formulario para crear un nuevo usuario.
        /// </summary>
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, "USER")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsUserAsync(currentUser, "Crear Usuario");

            var model = await _userService.GetUserForCreateAsync();

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo al formulario de Creación de Usuario | Resultado: {Resultado} |",
                currentUser.UserName, IpAddressForLogging, "Acceso concedido");

            return View(model);
        }

        /// <summary>
        /// Obtiene la lista de vistas y sus permisos asociados a un rol específico.
        /// Este endpoint es llamado vía AJAX desde el formulario de creación/edición de usuario.
        /// </summary>
        /// <param name="roleName">El nombre del rol seleccionado.</param>
        /// <returns>Un JSON con una lista de ViewPermissionViewModel.</returns>
        [HttpGet("GetViewsForRole")]
        [RequiredPermission(PermissionType.View, "USER")]
        public async Task<IActionResult> GetViewsForRole(string roleName)
        {
            var currentUser = await GetCurrentApplicationUserAsync();

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Obteniendo Vistas para Rol: {RolNombre} |",
                currentUser?.UserName ?? "N/A", IpAddressForLogging, roleName);
            if (string.IsNullOrWhiteSpace(roleName))
            {
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Intento de obtener vistas con nombre de rol vacío |",
                    currentUser?.UserName ?? "N/A", IpAddressForLogging); return BadRequest("El nombre del rol no puede ser nulo o vacío.");
            }

            var views = await _userService.GetViewsAndPermissionsForRoleAsync(roleName);
            return Json(views);
        }

        /// <summary>
        /// Procesa la creación de un nuevo usuario enviado desde el formulario.
        /// </summary>
        /// <remarks>
        /// Este endpoint es llamado vía AJAX. Realiza validaciones del modelo y la lógica de creación.
        /// Requiere permiso 'Create' para "USER".
        /// </remarks>
        /// <param name="model">El UserViewModel con los datos del nuevo usuario.</param>
        /// <returns>Un JSON ServiceResult indicando éxito, fracaso o errores de validación.</returns>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "USER")]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsUserAsync(currentUser, "Procesando Creación de Usuario");

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Creación de Usuario - Modelo Inválido | TipoEntidad: Usuario | Resultado: {Resultado} | Errores: {@Errores} |",
                    currentUser.UserName, IpAddressForLogging, "Validación Fallida", fieldErrors);
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
            }

            var result = await _userService.CreateUserAsync(model); 

            if (result.Success)
            {
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Usuario Creado Exitosamente | TipoEntidad: Usuario | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} |",
                    model.UserName, IpAddressForLogging, result.UserId, "Éxito");
                return Json(new { success = true, message = "Usuario creado exitosamente." });
            }
            else
            {
                _logger.LogError("Usuario: {Usuario} | IP: {IP} | Acción: Creación de Usuario Fallida | TipoEntidad: Usuario | ID_Entidad: {ID_Entidad} | Razón: {Mensaje} |",
                    model.UserName, IpAddressForLogging, result.UserId, result.Message);
                return Json(new { success = false, message = result.Message });
            }
        }

        /// <summary>
        /// Muestra el formulario para editar un usuario existente.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'Edit' para "USER".
        /// </remarks>
        /// <param name="id">El ID del usuario a editar.</param>
        /// <returns>La vista para el formulario de edición de usuario.</returns>
        [HttpGet("Edit/{id}")]
        [RequiredPermission(PermissionType.Edit, "USER")]
        public async Task<IActionResult> Edit(string id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsUserAsync(currentUser, "Editar Usuario");

            var userModel = await _userService.GetUserForEditAsync(id);

            if (userModel == null)
            {
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo a formulario de Edición - No encontrado o Prohibido | TipoEntidad: Usuario | ID_Entidad: {ID_Entidad} |",
                    currentUser.UserName, IpAddressForLogging, id);
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo a formulario de Edición | TipoEntidad: Usuario | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} |",
                currentUser.UserName, IpAddressForLogging, id, "Acceso concedido");
            return View(userModel);
        }

        /// <summary>
        /// Procesa la actualización de un usuario existente enviado desde el formulario.
        /// </summary>
        /// <remarks>
        /// Este endpoint es llamado vía AJAX. Realiza validaciones del modelo y la lógica de actualización.
        /// Requiere permiso 'Edit' para "USER".
        /// </remarks>
        /// <param name="id">El ID del usuario (desde la URL, para verificación).</param>
        /// <param name="model">El UserViewModel con los datos actualizados del usuario.</param>
        /// <returns>Un JSON ServiceResult indicando éxito, fracaso o errores de validación.</returns>
        [HttpPost("Edit/{id}")] // La ruta espera un ID
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "USER")]
        public async Task<IActionResult> Edit(string id, [FromForm] UserEditViewModel model)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsUserAsync(currentUser, "Procesando Edición de Usuario");

            if (id != model.Id)
            {
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Edición de Usuario - IDs No Coinciden | TipoEntidad: Usuario | ID_URL: {ID_URL} | ID_Modelo: {ID_Modelo} |",
                    currentUser.UserName, IpAddressForLogging, id, model.Id);
                return BadRequest(ServiceResult.FailureResult("El ID del usuario en la URL no coincide con el ID del formulario."));
            }

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Edición de Usuario - Modelo Inválido | TipoEntidad: Usuario | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} | Errores: {@Errores} |",
                    currentUser.UserName, IpAddressForLogging, model.Id, "Validación Fallida", fieldErrors);
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
            }

            var result = await _userService.UpdateUserAsync(model);

            if (result.Success)
            {
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Usuario Actualizado Exitosamente | TipoEntidad: Usuario | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} |",
                    currentUser.UserName, IpAddressForLogging, model.Id, "Éxito");            }
            else
                {
                _logger.LogError("Usuario: {Usuario} | IP: {IP} | Acción: Edición de Usuario Fallida | TipoEntidad: Usuario | ID_Entidad: {ID_Entidad} | Razón: {Mensaje} |",
                    currentUser.UserName, IpAddressForLogging, model.Id, result.Message);
            }

                return Json(result);
        }

        /// <summary>
        /// Muestra los detalles de un usuario específico.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'View' para "USER".
        /// </remarks>
        /// <param name="id">El ID del usuario a visualizar.</param>
        /// <returns>La vista con los detalles del usuario.</returns>
        [HttpGet("Detail/{id}")]
        [RequiredPermission(PermissionType.View, "USER")]
        public async Task<IActionResult> Detail(string id) // El ID de usuario es string
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsUserAsync(currentUser, "Detalles del Usuario");

            var userModel = await _userService.GetUserForDetailsAsync(id);

            if (userModel == null)
            {
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo a Detalles de Usuario - No Encontrado | TipoEntidad: Usuario | ID_Entidad: {ID_Entidad} |",
                    currentUser.UserName, IpAddressForLogging, id);
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo a Detalles de Usuario | TipoEntidad: Usuario | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} |",
                currentUser.UserName, IpAddressForLogging, id, "Acceso concedido");
            return View(userModel);
        }

        /// <summary>
        /// Exporta una lista filtrada de usuarios en el formato especificado.
        /// Este método es invocado mediante una solicitud HTTP GET y genera un archivo para descarga.
        /// </summary>
        /// <remarks>
        /// Los datos se filtran según los parámetros proporcionados.
        /// Requiere el permiso 'View' para "USER".
        /// </remarks>
        /// <param name="exportFormat">El formato de archivo deseado (ej. "excel", "csv", "pdf", "json").</param>
        /// <param name="searchTerm">Opcional. Término de búsqueda.</param>
        /// <param name="selectedRoleFilter">Opcional. Filtro por nombre de rol.</param>
        /// <param name="selectedBranchFilter">Opcional. Filtro por ID de sucursal.</param>
        /// <returns>Un <see cref="IActionResult"/> que devuelve un archivo para descarga en caso de éxito.</returns>
        [HttpGet("ExportUser")] // Nueva ruta específica para exportación de usuarios
        [RequiredPermission(PermissionType.View, "USER")]
        public async Task<IActionResult> ExportUser(
            string exportFormat,
            string? searchTerm = null, string? selectedRoleFilter = null, int? selectedBranchFilter = null)
        {
            ApplicationUser? currentUser = null;
            try
            {
                currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return Unauthorized();

                await SetCommonViewBagsUserAsync(currentUser, "Procesando Exportación de Usuarios");

                var usersToExport = await _userService.GetExportableUsersAsync(
                    searchTerm, selectedRoleFilter, selectedBranchFilter);

                if (usersToExport == null || !usersToExport.Any())
                {
                    _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Exportación de Usuarios - No se encontraron Datos | TipoEntidad: Usuario |",
                        currentUser.UserName, IpAddressForLogging);
                    return NotFound("No se encontraron usuarios con los filtros especificados para exportar.");
                }

                var columnDisplayNames = new Dictionary<string, string>
                {
                    {"Id", "ID de Usuario"},
                    {"UserName", "Nombre de Usuario"},
                    {"NombreUsuario", "Nombre Completo del Usuario"},
                    {"Email", "Correo Electrónico"},
                    {"SelectedRole", "Rol Asignado"},
                    {"AssignedBranchesNames", "Sucursales Asignadas"}
                };

                return await _exportService.ExportDataAsync(
                    usersToExport.ToList(),
                    exportFormat,
                    "USUARIOS",
                    columnDisplayNames
                );
            }
            catch (NotImplementedException ex)
            {
                _logger.LogWarning(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Exportación de Usuarios - Formato No Implementado | TipoEntidad: Usuario | Formato: {FormatoExportacion} |",
                    currentUser?.UserName ?? "N/A", IpAddressForLogging, exportFormat);
                return BadRequest($"El formato de exportación '{exportFormat}' no está implementado.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Exportación de Usuarios - Formato Inválido | TipoEntidad: Usuario | Formato: {FormatoExportacion} |",
                    currentUser?.UserName ?? "N/A", IpAddressForLogging, exportFormat);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Exportación de Usuarios - Error General | TipoEntidad: Usuario |",
                    currentUser?.UserName ?? "N/A", IpAddressForLogging);
                return StatusCode(500, "Ocurrió un error interno del servidor al exportar los datos.");
            }
        }
    }
}