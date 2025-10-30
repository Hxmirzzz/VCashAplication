using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCashApp.Controllers;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.ViewModels.Employee;
using VCashApp.Services;
using VCashApp.Services.DTOs;
using VCashApp.Services.Employee.Application;
using VCashApp.Services.Employee.Domain;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de empleados. Sigue un patrón de servicio para la lógica de negocio.
    /// Proporciona funcionalidades para listar, crear, editar y visualizar información de empleados.
    /// </summary>
    [Authorize]
    [Route("/Employee")]
    public class EmployeeController : BaseController
    {
        private readonly IEmployeeReadService _read;
        private readonly IEmployeeWriteService _write;
        private readonly IEmployeeFileStorage _storage;
        private readonly IExportService _export;
        private readonly ILogger<EmployeeController> _log;

        /// <summary>
        /// Constructor del controlador EmployeeController.
        /// </summary>
        /// <param name="read">Servicio para operaciones de lectura de empleados.</param>
        /// <param name="write">Servicio para operaciones de escritura de empleados.</param>
        /// <param name="storage">Servicio para operaciones de lectura de imagenes.</param> 
        /// <param name="export">Servicio para funcionalidades de exportación.</param>
        /// <param name="context">Contexto de la base de datos de la aplicación.</param>
        /// <param name="um">Administrador de usuarios para gestionar ApplicationUser.</param>
        /// <param name="log">Servicio de logging para el controlador.</param>
        public EmployeeController(
            AppDbContext context,
            UserManager<ApplicationUser> um,
            IEmployeeReadService read,
            IEmployeeWriteService write,
            IEmployeeFileStorage storage,
            IExportService export,
            ILogger<EmployeeController> log
        ) : base(context, um)
        {
            _read = read;
            _write = write;
            _storage = storage;
            _export = export;
            _log = log;
        }

        private async Task SetCommonViewBagsEmployeeAsync(ApplicationUser user, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(user, pageName);
            bool isAdmin = ViewBag.IsAdmin;

            var (cargos, sucursales, ciudades) = await _read.GetLookupsAsync(user.Id, isAdmin);
            ViewBag.Cargos = new SelectList(cargos, "Value", "Text");
            ViewBag.Sucursales = new SelectList(sucursales, "Value", "Text");
            ViewBag.Ciudades = new SelectList(ciudades, "Value", "Text");

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.HasCreatePermission = await HasPermisionForView(roles, "EMP", PermissionType.Create);
            ViewBag.HasEditPermission = await HasPermisionForView(roles, "EMP", PermissionType.Edit);
            ViewBag.HasViewPermission = await HasPermisionForView(roles, "EMP", PermissionType.View);
        }

        /// <summary>
        /// Muestra una lista paginada y filtrada de empleados.
        /// </summary>
        /// <remarks>
        /// Esta acción sirve la página principal de listado de empleados y puede devolver una vista parcial para solicitudes AJAX.
        /// Requiere permiso 'View' para "EMP".
        /// </remarks>
        /// <param name="page">Número de página actual (default 1).</param>
        /// <param name="pageSize">Número de empleados por página (default 15).</param>
        /// <param name="cargoId">Opcional. Código del cargo por el cual filtrar.</param>
        /// <param name="branchId">Opcional. Código de la sucursal por la cual filtrar.</param>
        /// <param name="employeeStatus">Opcional. Estado del empleado por el cual filtrar.</param>
        /// <param name="search">Opcional. Término de búsqueda (cédula o nombre).</param>
        /// <param name="gender">Opcional. Género por el cual filtrar.</param>
        /// <returns>La vista con la tabla de empleados paginada.</returns>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "EMP")]
        public async Task<IActionResult> Index(int? cargoId, int? branchId, int? employeeStatus, string? search, string? gender, int page = 1, int pageSize = 15)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeAsync(user, "Empleados");
            bool isAdmin = ViewBag.IsAdmin;

            var (items, total) = await _read.GetPagedAsync(
                user.Id, cargoId, branchId, employeeStatus, search, gender, page, pageSize, isAdmin);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.TotalData = total;
            ViewBag.SearchTerm = search;
            ViewBag.CurrentCargoId = cargoId;
            ViewBag.CurrentBranchId = branchId;
            ViewBag.CurrentEmployeeStatus = employeeStatus;
            ViewBag.CurrentGender = gender;

            _log.LogInformation("User {u} IP {ip} -> Employee.Index count={n}",
                user.UserName, IpAddressForLogging, items.Count());

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("~/Views/Employee/_EmployeeTablePartial.cshtml", items);

            return View("~/Views/Employee/Index.cshtml", items);
        }

        /// <summary>
        /// Muestra el formulario para crear un nuevo empleado.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'Create' para "EMP".
        /// </remarks>
        /// <returns>La vista para el formulario de creación de empleado.</returns>
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, "EMP")]
        public async Task<IActionResult> Create()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeAsync(user, "Crear Empleado");
            bool isAdmin = ViewBag.IsAdmin is bool b && b;

            var lookups = await _read.GetLookupsAsync(user.Id, isAdmin);

            var vm = new EmployeeViewModel
            {
                Cargos = lookups.Cargos,
                Sucursales = lookups.Sucursales,
                Ciudades = lookups.Ciudades,
                IndicadorCatalogo = false,
                IngresoRepublica = false,
                IngresoAeropuerto = false,
                FechaVinculacion = DateOnly.FromDateTime(DateTime.Today),
                EmployeeStatus = (int)EstadoEmpleado.Activo
            };

            return View("~/Views/Employee/Create.cshtml", vm);
        }

        /// <summary>
        /// Procesa la creación de un nuevo empleado.
        /// </summary>
        /// <remarks>
        /// Este endpoint es llamado vía AJAX. Realiza validaciones del modelo y la lógica de creación.
        /// Requiere permiso 'Create' para "EMP".
        /// </remarks>
        /// <param name="model">El EmpleadoViewModel con los datos del nuevo empleado.</param>
        /// <returns>Un JSON ServiceResult indicando éxito, fracaso o errores de validación.</returns>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "EMP")]
        public async Task<IActionResult> Create([FromForm] EmployeeViewModel model)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(k => k.Value!.Errors.Any())
                    .ToDictionary(
                        k => k.Key,
                        v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors));
            }

            var result = await _write.CreateAsync(model, user.Id);
            return Json(result);
        }

        /// <summary>
        /// Muestra el formulario para editar un empleado existente.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'Edit' para "EMP".
        /// </remarks>
        /// <param name="id">El ID del empleado a editar.</param>
        /// <returns>La vista para el formulario de edición de empleado.</returns>
        [HttpGet("Edit/{id:int}")]
        [RequiredPermission(PermissionType.Edit, "EMP")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeAsync(user, "Editar Empleado");
            bool isAdmin = ViewBag.IsAdmin is bool b && b;

            var vm = await _read.GetForEditAsync(id, user.Id, isAdmin);
            if (vm is null)
            {
                TempData["ErrorMessage"] = "Empleado no encontrado o no tiene permiso para verlo.";
                return RedirectToAction(nameof(Index));
            }

            var lookups = await _read.GetLookupsAsync(user.Id, isAdmin);
            vm.Cargos = lookups.Cargos;
            vm.Sucursales = lookups.Sucursales;
            vm.Ciudades = lookups.Ciudades;

            return View("~/Views/Employee/Edit.cshtml", vm);
        }

        /// <summary>
        /// Procesa la actualización de un empleado existente.
        /// </summary>
        /// <remarks>
        /// Este endpoint es llamado vía AJAX. Realiza validaciones del modelo y la lógica de actualización.
        /// Requiere permiso 'Edit' para "EMP".
        /// </remarks>
        /// <param name="id">El ID del empleado (desde la URL, para verificación).</param>
        /// <param name="model">El EmpleadoViewModel con los datos actualizados del empleado.</param>
        /// <returns>Un JSON ServiceResult indicando éxito, fracaso o errores de validación.</returns>
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "EMP")]
        public async Task<IActionResult> Edit([FromRoute] int id, [FromForm] EmployeeViewModel model)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return Unauthorized();

            if (id != model.CodCedula)
                return BadRequest(ServiceResult.FailureResult("El ID de la URL no coincide con el del formulario."));

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(k => k.Value!.Errors.Any())
                    .ToDictionary(
                        k => k.Key,
                        v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors));
            }

            var result = await _write.UpdateAsync(model, user.Id);
            return Json(result);
        }

        /// <summary>
        /// Endpoint para cambiar el estado de un empleado.
        /// </summary>
        /// <param name="id">El ID del empleado (CodCedula).</param>
        /// <param name="EmployeeId">Empleado.</param>
        /// <param name="NewStatus">Nuevo estado.</param>
        /// <param name="ReasonForChange">Razon del cambio de estado.</param>
        /// <returns>Un JSON ServiceResult.</returns>
        [HttpPost("ChangeStatus/{id:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "EMP")]
        public async Task<IActionResult> ChangeStatus(
            [FromRoute] int id,
            [FromForm] int EmployeeId,
            [FromForm] int NewStatus,
            [FromForm] string ReasonForChange)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return Unauthorized();

            if (id != EmployeeId)
                return BadRequest(ServiceResult.FailureResult("El ID de la URL no coincide con el del formulario."));

            var result = await _write.ChangeStatusAsync(EmployeeId, NewStatus, ReasonForChange, user.Id);
            return Json(result);
        }

        ///<summary>
        ///Muestra un formulario para ver los detalles de un empleado.
        ///</summary>
        /// <remarks>
        /// Requiere permiso 'View' para "EMP".
        /// </remarks>
        /// <param name="id">El ID del empleado a visualizar.</param>
        /// <returns>La vista con los detalles del empleado.</returns>
        [HttpGet("Detail/{id:int}")]
        [RequiredPermission(PermissionType.View, "EMP")]
        public async Task<IActionResult> Detail(int id)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeAsync(user, "Detalles del Empleado");
            bool isAdmin = ViewBag.IsAdmin is bool b && b;

            var vm = await _read.GetForDetailsAsync(id, user.Id, ViewBag.IsAdmin);
            if (vm is null)
            {
                TempData["ErrorMessage"] = "Empleado no encontrado o no autorizado.";
                return RedirectToAction(nameof(Index));
            }

            var lookups = await _read.GetLookupsAsync(user.Id, isAdmin);
            vm.Cargos = lookups.Cargos;
            vm.Sucursales = lookups.Sucursales;
            vm.Ciudades = lookups.Ciudades;

            return View("~/Views/Employee/Detail.cshtml", vm);
        }

        /// <summary>
        /// Exporta una lista filtrada de empleados en el formato especificado.
        /// Este método es invocado mediante una solicitud HTTP GET y genera un archivo para descarga.
        /// </summary>
        /// <remarks>
        /// Los datos se filtran según los parámetros proporcionados y los permisos de sucursal del usuario actual.
        /// Se obtiene la totalidad de los registros que coinciden con los filtros para la exportación.
        /// Requiere el permiso 'View' para "EMP".
        /// </remarks>
        /// <param name="exportFormat">
        /// El formato de archivo deseado para la exportación (ej. "excel", "csv", "pdf", "json").
        /// </param>
        /// <param name="cargoId">
        /// Opcional. El código del cargo por el cual filtrar los empleados.
        /// </param>
        /// <param name="branchId">
        /// Opcional. El código de la sucursal por la cual filtrar los empleados.
        /// </param>
        /// <param name="employeeStatus">
        /// Opcional. El estado del empleado por el cual filtrar (valor numérico del enum EstadoEmpleado).
        /// </param>
        /// <param name="search">
        /// Opcional. Término de búsqueda para filtrar por cédula, nombre o apellido.
        /// </param>
        /// <param name="gender">
        /// Opcional. Género por el cual filtrar (ej. "M", "F", "O").
        /// </param>
        /// <returns>
        /// Un <see cref="IActionResult"/> que devuelve un archivo para descarga en caso de éxito.
        /// Retorna <see cref="UnauthorizedResult"/> si el usuario no está autenticado.
        /// Retorna <see cref="NotFoundObjectResult"/> si no se encuentran empleados con los filtros.
        /// Retorna <see cref="BadRequestObjectResult"/> si el formato de exportación no es soportado o no está implementado.
        /// Retorna <see cref="StatusCodeResult"/> (500) en caso de un error interno del servidor.
        /// </returns>
        [HttpGet("ExportEmployee")]
        [RequiredPermission(PermissionType.View, "EMP")]
        public async Task<IActionResult> ExportEmployee(
            string exportFormat,
            int? cargoId,
            int? branchId,
            int? employeeStatus,
            string? search,
            string? gender)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return Unauthorized();

            try
            {
                await SetCommonViewBagsEmployeeAsync(user, "Exportar Empleados");

                bool isAdmin = ViewBag.IsAdmin is bool b && b;

                var pageProbe = await _read.GetPagedAsync(
                    user.Id, cargoId, branchId, employeeStatus, search, gender,
                    page: 1, pageSize: 1, isAdmin: isAdmin);

                int total = pageProbe.Total;
                if (total == 0)
                    return NotFound("No se encontraron empleados con los filtros especificados.");

                var pageAll = await _read.GetPagedAsync(
                    user.Id, cargoId, branchId, employeeStatus, search, gender,
                    page: 1, pageSize: total, isAdmin: isAdmin);

                var items = pageAll.Items.ToList();

                var columns = new Dictionary<string, string>
        {
            { "CodCedula", "Cédula" },
            { "TipoDocumento", "Tipo Documento" },
            { "NumeroCarnet", "Número Carnet" },
            { "FirstName", "Primer Nombre" },
            { "MiddleName", "Segundo Nombre" },
            { "FirstLastName", "Primer Apellido" },
            { "SecondLastName", "Segundo Apellido" },
            { "NombreCompleto", "Nombre Completo" },
            { "FechaNacimiento", "Fecha Nacimiento" },
            { "FechaExpedicion", "Fecha Expedición" },
            { "NombreCiudadExpedicion", "Ciudad Expedición" },
            { "NombreCargo", "Cargo" },
            { "NombreUnidad", "Unidad" },
            { "NombreSucursal", "Sucursal" },
            { "Celular", "Celular" },
            { "Direccion", "Dirección" },
            { "Correo", "Correo" },
            { "BloodType", "RH" },
            { "Genero", "Género" },
            { "OtroGenero", "Otro Género" },
            { "FechaVinculacion", "Fecha Vinculación" },
            { "FechaRetiro", "Fecha Retiro" },
            { "IndicadorCatalogo", "Indicador Catálogo" },
            { "IngresoRepublica", "Ingreso República" },
            { "IngresoAeropuerto", "Ingreso Aeropuerto" },
            { "EmployeeStatus", "Estado" }
        };

                return await _export.ExportDataAsync(items, exportFormat, "EMPLEADOS", columns);
            }
            catch (NotImplementedException)
            {
                return BadRequest($"El formato de exportación '{exportFormat}' no está implementado.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error interno del servidor al exportar los datos.");
            }
        }

        /// <summary>
        /// Sirve archivos de imagen (fotos o firmas) de empleados desde el repositorio.
        /// </summary>
        [HttpGet("/Employee/images/{*filePath}")]
        public async Task<IActionResult> GetImage(string filePath)
        {
            var stream = await _storage.OpenReadAsync(filePath);
            if (stream is null) return NotFound();

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var ct)) ct = "application/octet-stream";
            return File(stream, ct);
        }
    }
}