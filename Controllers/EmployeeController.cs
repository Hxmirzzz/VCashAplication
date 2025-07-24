using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    /// Controlador para la gestión de empleados. Sigue un patrón de servicio para la lógica de negocio.
    /// Proporciona funcionalidades para listar, crear, editar y visualizar información de empleados.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("/Employee")]
    public class EmployeeController : BaseController
    {
        private readonly IEmployeeService _employeeService;
        private readonly IExportService _exportService;
        private readonly ILogger<EmployeeController> _logger;

        /// <summary>
        /// Constructor del controlador EmployeeController.
        /// </summary>
        public EmployeeController(
            IEmployeeService employeeService,
            IExportService exportService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<EmployeeController> logger)
            : base(context, userManager)
        {
            _employeeService = employeeService;
            _exportService = exportService;
            _logger = logger;
        }

        // Método auxiliar para configurar ViewBags comunes
        private async Task SetCommonViewBagsEmployeeAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            bool isAdmin = (bool)ViewBag.IsAdmin;
            var (cargos, sucursales, ciudades) = await _employeeService.GetDropdownListsAsync(currentUser.Id, isAdmin);

            ViewBag.Cargos = new SelectList(cargos, "Value", "Text");
            ViewBag.Sucursales = new SelectList(sucursales, "Value", "Text");
            ViewBag.Ciudades = new SelectList(ciudades, "Value", "Text");

            // Obtener permisos
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreatePermission = await HasPermisionForView(userRoles, "EMP", PermissionType.Create);
            ViewBag.HasEditPermission = await HasPermisionForView(userRoles, "EMP", PermissionType.Edit);
            ViewBag.HasViewPermission = await HasPermisionForView(userRoles, "EMP", PermissionType.View);
        }

        /// <summary>
        /// Muestra una lista paginada y filtrada de empleados.
        /// </summary>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "EMP")]
        public async Task<IActionResult> Index(int? cargoId, int? branchId, int? employeeStatus, string? search, string? gender, int page = 1, int pageSize = 15)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeAsync(currentUser, "Empleados");
            bool isAdmin = (bool)ViewBag.IsAdmin;

            var (data, totalData) = await _employeeService.GetFilteredEmployeesAsync(
                currentUser.Id,
                cargoId,
                branchId,
                employeeStatus,
                search,
                gender,
                page,
                pageSize,
                isAdmin
            );

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalData / pageSize);
            ViewBag.TotalData = totalData;
            ViewBag.SearchTerm = search;
            ViewBag.CurrentCargoId = cargoId;
            ViewBag.CurrentBranchId = branchId;
            ViewBag.CurrentEmployeeStatus = employeeStatus;
            ViewBag.CurrentGender = gender;

            _logger.LogInformation("| User: {User} | IP: {Ip} | Action: Accessing Employee List | Count: {Count} | Result: Access Granted |", currentUser.UserName, IpAddressForLogging, data.Count());

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EmployeeTablePartial", data);
            }

            return View(data);
        }

        /// <summary>
        /// Muestra el formulario para crear un nuevo empleado.
        /// </summary>
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, "EMP")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsBaseAsync(currentUser, "Crear Empleado");
            bool isAdmin = (bool)ViewBag.IsAdmin;

            var (cargos, sucursales, ciudades) = await _employeeService.GetDropdownListsAsync(currentUser.Id, isAdmin);

            var model = new EmpleadoViewModel
            {
                Cargos = cargos,
                Sucursales = sucursales,
                Ciudades = ciudades,
                IndicadorCatalogo = false,
                IngresoRepublica = false,
                IngresoAeropuerto = false,
                FechaVinculacion = DateOnly.FromDateTime(DateTime.Now),
                EmployeeStatus = (int)EstadoEmpleado.Activo
            };

            _logger.LogInformation("| User: {User} | IP: {Ip} | Action: Accessing Create Employee Form | Result: Access Granted |", currentUser.UserName, IpAddressForLogging);

            return View(model); // Retorna el ViewModel con las listas
        }

        /// <summary>
        /// Procesa la creación de un nuevo empleado.
        /// </summary>
        // En EmployeeController.cs

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "EMP")]
        public async Task<IActionResult> Create([FromForm] EmpleadoViewModel model)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("| User: {User} | IP: {Ip} | Action: Create Employee - Model Invalid | Errors: {@Errors} |", currentUser.UserName, IpAddressForLogging, fieldErrors);
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
            }

            var result = await _employeeService.CreateEmployeeAsync(model, currentUser.Id);
            return Json(result);
        }

        /// <summary>
        /// Muestra el formulario para editar un empleado existente.
        /// </summary>
        [HttpGet("Edit/{id}")]
        [RequiredPermission(PermissionType.Edit, "EMP")]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsBaseAsync(currentUser, "Editar Empleado");
            bool isAdmin = (bool)ViewBag.IsAdmin;

            var employeeModel = await _employeeService.GetEmployeeForEditAsync(id, currentUser.Id, isAdmin);

            if (employeeModel == null)
            {
                _logger.LogWarning("| User: {User} | IP: {Ip} | Action: Access Edit Employee Form - Not Found or Forbidden | EmployeeId: {EmployeeId} |", currentUser.UserName, IpAddressForLogging, id);
                TempData["ErrorMessage"] = "Empleado no encontrado o no tiene permiso para verlo.";
                return RedirectToAction(nameof(Index));
            }

            var (cargos, sucursales, ciudades) = await _employeeService.GetDropdownListsAsync(currentUser.Id, isAdmin);
            employeeModel.Cargos = cargos;
            employeeModel.Sucursales = sucursales;
            employeeModel.Ciudades = ciudades;

            _logger.LogInformation("| User: {User} | IP: {Ip} | Action: Accessing Edit Employee Form | EmployeeId: {EmployeeId} | Result: Access Granted |", currentUser.UserName, IpAddressForLogging, id);
            return View(employeeModel);
        }

        /// <summary>
        /// Procesa la actualización de un empleado existente.
        /// </summary>
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "EMP")]
        public async Task<IActionResult> Edit(int id, [FromForm] EmpleadoViewModel model)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsEmployeeAsync(currentUser, "Procesando Edición de Empleado");

            if (id != model.CodCedula)
            {
                return BadRequest(ServiceResult.FailureResult("El ID del empleado en la URL no coincide con el ID del formulario."));
            }

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("| User: {User} | IP: {Ip} | Action: Edit Employee - Model Invalid | Errors: {@Errors} |", currentUser.UserName, IpAddressForLogging, fieldErrors);
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
            }

            var result = await _employeeService.UpdateEmployeeAsync(model, currentUser.Id);

            if (result.Success)
            {
                _logger.LogInformation("| User: {User} | IP: {Ip} | Action: Employee Updated | EmployeeId: {EmployeeId} | Result: Success |", currentUser.UserName, IpAddressForLogging, model.CodCedula);
            }
            else
            {
                _logger.LogError("| User: {User} | IP: {Ip} | Action: Employee Update Failed | EmployeeId: {EmployeeId} | Result: {Message} |", currentUser.UserName, IpAddressForLogging, model.CodCedula, result.Message);
            }

            return Json(result);
        }

        /// <summary>
        /// Endpoint para cambiar el estado de un empleado.
        /// </summary>
        /// <param name="id">El ID del empleado (CodCedula).</param>
        /// <param name="statusChangeRequest">Un DTO que contiene el nuevo estado y la razón.</param>
        /// <returns>Un JSON ServiceResult.</returns>
        [HttpPost("ChangeStatus/{id}")]
        [ValidateAntiForgeryToken] // Aunque se envía por JS, es buena práctica mantenerlo para seguridad
        [RequiredPermission(PermissionType.Edit, "EMP")] // Asumo que el permiso para editar también aplica para cambiar estado
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] StatusChangeRequestDTO statusChangeRequest)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsEmployeeAsync(currentUser, "Procesando Cambio de Estado de Empleado");

            if (id != statusChangeRequest.EmployeeId)
            {
                return BadRequest(ServiceResult.FailureResult("El ID del empleado en la URL no coincide con el ID del cuerpo de la solicitud."));
            }

            var result = await _employeeService.ChangeEmployeeStatusAsync(
                id,
                statusChangeRequest.NewStatus,
                statusChangeRequest.ReasonForChange,
                currentUser.Id
            );

            if (result.Success)
            {
                _logger.LogInformation("| User: {User} | IP: {Ip} | Action: Employee Status Changed | EmployeeId: {EmployeeId} | Result: Success |", currentUser.UserName, IpAddressForLogging, id);
            }
            else
            {
                _logger.LogError("| User: {User} | IP: {Ip} | Action: Employee Status Change Failed | EmployeeId: {EmployeeId} | Reason: {Message} |", currentUser.UserName, IpAddressForLogging, id, result.Message);
            }

            return Json(result);
        }

        ///<summary>
        ///Muestra un formulario para ver los detalles de un empleado.
        ///</summary>
        [HttpGet("Detail/{id}")]
        [RequiredPermission(PermissionType.View, "EMP")]
        public async Task<IActionResult> Detail(int id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsBaseAsync(currentUser, "Detalles del Empleado");
            bool isAdmin = (bool)ViewBag.IsAdmin;

            var employeeModel = await _employeeService.GetEmployeeForDetailsAsync(id, currentUser.Id, isAdmin);

            if (employeeModel == null)
            {
                _logger.LogWarning("| User: {User} | IP: {Ip} | Action: Access Employee Details - Not Found or Forbidden | EmployeeId: {EmployeeId} |", currentUser.UserName, IpAddressForLogging, id);
                TempData["ErrorMessage"] = "Empleado no encontrado o no tiene permiso para verlo.";
                return RedirectToAction(nameof(Index));
            }

            var (cargos, sucursales, ciudades) = await _employeeService.GetDropdownListsAsync(currentUser.Id, isAdmin);
            employeeModel.Cargos = cargos;
            employeeModel.Sucursales = sucursales;
            employeeModel.Ciudades = ciudades;

            _logger.LogInformation("| User: {User} | IP: {Ip} | Action: Accessing Employee Details | EmployeeId: {EmployeeId} | Result: Access Granted |", currentUser.UserName, IpAddressForLogging, id);
            return View(employeeModel);
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
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            try
            {
                await SetCommonViewBagsEmployeeAsync(currentUser, "Procesando Exportación de Empleados");

                bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                var employeesToExport = await _employeeService.GetExportableEmployeesAsync(
                    currentUser.Id,
                    cargoId,
                    branchId,
                    employeeStatus,
                    search,
                    gender,
                    isAdmin
                );

                if (employeesToExport == null || !employeesToExport.Any())
                {
                    _logger.LogInformation("| User: {User} | IP: {Ip} | Action: Export Employees - No Data Found |", currentUser.UserName, IpAddressForLogging);
                    return NotFound("No se encontraron empleados con los filtros especificados para exportar.");
                }

                var columnDisplayNames = new Dictionary<string, string>
                {
                    {"CodCedula", "Cédula"},
                    {"TipoDocumento", "Tipo Documento"},
                    {"NumeroCarnet", "Número Carnet"},
                    {"FirstName", "Primer Nombre"},
                    {"MiddleName", "Segundo Nombre"},
                    {"FirstLastName", "Primer Apellido"},
                    {"SecondLastName", "Segundo Apellido"},
                    {"NombreCompleto", "Nombre Completo"},
                    {"FechaNacimiento", "Fecha Nacimiento"},
                    {"FechaExpedicion", "Fecha Expedición"},
                    {"NombreCiudadExpedicion", "Ciudad Expedición"}, 
                    {"NombreCargo", "Cargo"},
                    {"NombreUnidad", "Unidad"},
                    {"NombreSucursal", "Sucursal"},
                    {"Celular", "Celular"},
                    {"Direccion", "Dirección"},
                    {"Correo", "Correo"},
                    {"BloodType", "RH"},
                    {"Genero", "Género"},
                    {"OtroGenero", "Otro Género"},
                    {"FechaVinculacion", "Fecha Vinculación"},
                    {"FechaRetiro", "Fecha Retiro"},
                    {"IndicadorCatalogo", "Indicador Catálogo"},
                    {"IngresoRepublica", "Ingreso República"},
                    {"IngresoAeropuerto", "Ingreso Aeropuerto"},
                    {"EmployeeStatus", "Estado"}
                };

                return await _exportService.ExportDataAsync(
                    employeesToExport.ToList(),
                    exportFormat,
                    "EMPLEADOS",
                    columnDisplayNames
                );
            }
            catch (NotImplementedException ex)
            {
                _logger.LogWarning(ex, "| User: {User} | IP: {Ip} | Action: Export Employees - Format Not Implemented | Format: {ExportFormat} |", currentUser.UserName, IpAddressForLogging, exportFormat);
                return BadRequest($"El formato de exportación '{exportFormat}' no está implementado.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "| User: {User} | IP: {Ip} | Action: Export Employees - Invalid Format | Format: {ExportFormat} |", currentUser.UserName, IpAddressForLogging, exportFormat);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "| User: {User} | IP: {Ip} | Action: Export Employees - General Error |", currentUser.UserName, IpAddressForLogging);
                return StatusCode(500, "Ocurrió un error interno del servidor al exportar los datos.");
            }
        }

        /// <summary>
        /// Sirve archivos de imagen (fotos o firmas) de empleados desde el repositorio.
        /// </summary>
        [HttpGet("images/{*filePath}")]
        public async Task<IActionResult> GetImage(string filePath)
        {
            var fileStream = await _employeeService.GetEmployeeImageStreamAsync(filePath);
            if (fileStream == null)
            {
                return NotFound();
            }

            string mimeType = "image/jpeg";
            return File(fileStream, mimeType);
        }
    }
}