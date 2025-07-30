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
        /// <param name="employeeService">Servicio para la lógica de negocio de empleados.</param>
        /// <param name="exportService">Servicio para funcionalidades de exportación.</param>
        /// <param name="context">Contexto de la base de datos de la aplicación.</param>
        /// <param name="userManager">Administrador de usuarios para gestionar ApplicationUser.</param>
        /// <param name="logger">Servicio de logging para el controlador.</param>
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

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo a la lista de Empleados | Conteo: {Conteo} | Resultado: {Resultado} |",
                currentUser.UserName, IpAddressForLogging, data.Count(), "Acceso concedido");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EmployeeTablePartial", data);
            }

            return View(data);
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

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo al formulario de Creación de Empleado | Resultado: {Resultado} |",
                currentUser.UserName, IpAddressForLogging, "Acceso concedido");

            return View(model);
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
        public async Task<IActionResult> Create([FromForm] EmpleadoViewModel model)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsEmployeeAsync(currentUser, "Procesando Creación de Empleado");

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Creación de Empleado - Modelo Inválido | TipoEntidad: Empleado | Resultado: {Resultado} | Errores: {@Errores} |",
                    currentUser.UserName, IpAddressForLogging, "Validación Fallida", fieldErrors);
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
            }

            var result = await _employeeService.CreateEmployeeAsync(model, currentUser.Id);

            if (result.Success)
            {
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Empleado Creado Exitosamente | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} |",
                    currentUser.UserName, IpAddressForLogging, model.CodCedula, "Éxito");
            }
            else
                {
                _logger.LogError("Usuario: {Usuario} | IP: {IP} | Acción: Creación de Empleado Fallida | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} | Razón: {Mensaje} |",
                    currentUser.UserName, IpAddressForLogging, model.CodCedula, result.Message);
            }

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
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo a formulario de Edición - No encontrado o Prohibido | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} |",
                    currentUser.UserName, IpAddressForLogging, id);
                TempData["ErrorMessage"] = "Empleado no encontrado o no tiene permiso para verlo.";
                return RedirectToAction(nameof(Index));
            }

            var (cargos, sucursales, ciudades) = await _employeeService.GetDropdownListsAsync(currentUser.Id, isAdmin);
            employeeModel.Cargos = cargos;
            employeeModel.Sucursales = sucursales;
            employeeModel.Ciudades = ciudades;

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo a formulario de Edición | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} |",
                currentUser.UserName, IpAddressForLogging, id, "Acceso concedido");
            return View(employeeModel);
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
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Edición de Empleado - IDs No Coinciden | TipoEntidad: Empleado | ID_URL: {ID_URL}, ID_Modelo: {ID_Modelo} |",
                    currentUser.UserName, IpAddressForLogging, id, model.CodCedula);
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
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Edición de Empleado - Modelo Inválido | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} | Errores: {@Errores} |",
                    currentUser.UserName, IpAddressForLogging, model.CodCedula, "Validación Fallida", fieldErrors);
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
            }

            var result = await _employeeService.UpdateEmployeeAsync(model, currentUser.Id);

            if (result.Success)
            {
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Empleado Actualizado Exitosamente | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} |",
                    currentUser.UserName, IpAddressForLogging, model.CodCedula, "Éxito");
            }
            else
                {
                _logger.LogError("Usuario: {Usuario} | IP: {IP} | Acción: Edición de Empleado Fallida | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} | Razón: {Mensaje} |",
                    currentUser.UserName, IpAddressForLogging, model.CodCedula, result.Message);
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
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "EMP")]
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
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Estado de Empleado Cambiado | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} |",
                    currentUser.UserName, IpAddressForLogging, id, "Éxito");
            }
            else
            {
                _logger.LogError("Usuario: {Usuario} | IP: {IP} | Acción: Cambio de Estado de Empleado Fallido | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} | Razón: {Mensaje} |",
                    currentUser.UserName, IpAddressForLogging, id, result.Message);
            }

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
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Acceso a Detalles de Empleado - No encontrado o Prohibido | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} |",
                    currentUser.UserName, IpAddressForLogging, id);
                TempData["ErrorMessage"] = "Empleado no encontrado o no tiene permiso para verlo.";
                return RedirectToAction(nameof(Index));
            }

            var (cargos, sucursales, ciudades) = await _employeeService.GetDropdownListsAsync(currentUser.Id, isAdmin);
            employeeModel.Cargos = cargos;
            employeeModel.Sucursales = sucursales;
            employeeModel.Ciudades = ciudades;

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Accediendo a Detalles de Empleado | TipoEntidad: Empleado | ID_Entidad: {ID_Entidad} | Resultado: {Resultado} |",
                currentUser.UserName, IpAddressForLogging, id, "Acceso concedido");
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
                    _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Exportación de Empleados - No se encontraron Datos | TipoEntidad: Empleado |",
                        currentUser.UserName, IpAddressForLogging);
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
                _logger.LogWarning(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Exportación de Empleados - Formato No Implementado | TipoEntidad: Empleado | Formato: {FormatoExportacion} |",
                    currentUser?.UserName ?? "N/A", IpAddressForLogging, exportFormat);
                return BadRequest($"El formato de exportación '{exportFormat}' no está implementado.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Exportación de Empleados - Formato Inválido | TipoEntidad: Empleado | Formato: {FormatoExportacion} |",
                    currentUser?.UserName ?? "N/A", IpAddressForLogging, exportFormat);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Exportación de Empleados - Error General | TipoEntidad: Empleado |",
                    currentUser?.UserName ?? "N/A", IpAddressForLogging);
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