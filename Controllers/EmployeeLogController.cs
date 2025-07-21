using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.Entities;
using VCashApp.Services;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de registros de entrada y salida de empleados.
    /// Incluye funcionalidades de registro, actualización, historial y búsqueda de empleados.
    /// </summary>
    [ApiController]
    [Route("/EmployeeLog")] 
    public class EmployeeLogController : BaseController
    {
        private readonly IEmployeeLogService _employeeLogService;

        public EmployeeLogController(
            IEmployeeLogService employeeLogService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager
        ) : base(context, userManager)
        {
            _employeeLogService = employeeLogService;
        }

        // Método auxiliar para configurar ViewBags comunes específicos de este controlador
        private async Task SetCommonViewBagsEmployeeLogAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            var cargos = await _context.AdmCargos.ToListAsync();
            var unidades = await _context.AdmUnidades.ToListAsync();
            var sucursales = await _context.AdmSucursales.Where(s => s.Estado == true).ToListAsync();

            var cargosSelectListItems = cargos.Select(c => new SelectListItem
            {
                Value = c.CodCargo.ToString(),
                Text = c.NombreCargo
            }).ToList();
            cargosSelectListItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona Cargo --" });
            ViewBag.Cargos = new SelectList(cargosSelectListItems, "Value", "Text", ViewBag.CurrentCargoId);

            var unidadesSelectListItems = unidades.Select(u => new SelectListItem
            {
                Value = u.CodUnidad,
                Text = u.NombreUnidad
            }).ToList();

            unidadesSelectListItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona Unidad --" });
            ViewBag.Unidades = new SelectList(unidadesSelectListItems, "Value", "Text", ViewBag.CurrentUnitId);

            var sucursalesSelectListItems = sucursales.Select(s => new SelectListItem
            {
                Value = s.CodSucursal.ToString(),
                Text = s.NombreSucursal
            }).ToList();

            sucursalesSelectListItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona Sucursal --" });
            ViewBag.Sucursales = new SelectList(sucursalesSelectListItems, "Value", "Text", ViewBag.CurrentBranchId);

            var logStatusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Selecciona Estado --" },
                new SelectListItem { Value = "1", Text = "Completo" },
                new SelectListItem { Value = "0", Text = "Entrada Abierta" }
            };
                    ViewBag.LogStatusFilterList = new SelectList(logStatusList, "Value", "Text", ViewBag.CurrentLogStatus);

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.CanCreateLog = await HasPermisionForView(userRoles, "REG", PermissionType.Create);
            ViewBag.CanEditLog = await HasPermisionForView(userRoles, "REG", PermissionType.Edit);
            ViewBag.CanViewHistory = await HasPermisionForView(userRoles, "REGHIS", PermissionType.View);
        }

        /// <summary>
        /// Muestra el formulario para registrar la entrada o salida de un empleado.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Create' para la vista 'REG'.
        /// </remarks>
        /// <returns>La vista del formulario de registro de empleados.</returns>
        [HttpGet("LogEntry")]
        [RequiredPermission(PermissionType.Create, "REG")]
        public async Task<IActionResult> LogEntry()
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeLogAsync(currentUser, "Employee Log");

            Log.Information("| User: {User} | IP: {Ip} | Action: Accessing Employee Log Entry Form | Result: Access Granted |", currentUser.UserName, ViewBag.Ip);
            return View();
        }

        /// <summary>
        /// Busca empleados por cédula o nombre para la funcionalidad de autocompletado en el formulario de registro.
        /// </summary>
        /// <remarks>
        /// Esta es una llamada AJAX que devuelve una lista de empleados activos que coinciden con el término de búsqueda,
        /// filtrados por las sucursales permitidas del usuario.
        /// </remarks>
        /// <param name="searchInput">Término de búsqueda (puede ser cédula parcial o nombre/apellido).</param>
        /// <returns>Un JSON con una lista de objetos anónimos que representan la información de los empleados.</returns>
        [HttpGet("GetEmployeeInfo")]
        public async Task<IActionResult> GetEmployeeInfo(string searchInput)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            try
            {
                if (string.IsNullOrWhiteSpace(searchInput))
                {
                    return Json(new { error = "El campo de búsqueda no puede estar vacío." });
                }

                bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                List<int> permittedBranchIds = new List<int>();
                if (!isAdmin)
                {
                    var userClaims = await _context.UserClaims
                                                   .Where(uc => uc.UserId == currentUser.Id && uc.ClaimType == "SucursalId")
                                                   .Select(uc => uc.ClaimValue)
                                                   .ToListAsync();
                    permittedBranchIds = new List<int>();
                    foreach (var claimValue in userClaims)
                    {
                        if (int.TryParse(claimValue, out int parsedId))
                        {
                            permittedBranchIds.Add(parsedId);
                        }
                    }

                    if (!permittedBranchIds.Any())
                    {
                        return Json(new { error = "No tiene sucursales asignadas para buscar empleados." });
                    }
                }
                else
                {
                    permittedBranchIds = await _context.AdmSucursales.Where(s => s.Estado == true).Select(s => s.CodSucursal).ToListAsync();
                    if (!permittedBranchIds.Any())
                    {
                        return Json(new { error = "No hay sucursales activas en el sistema para buscar empleados (Administrador)." });
                    }
                }

                var employees = await _context.GetEmployeeInfoFromSpAsync(
                    currentUser.Id,
                    permittedBranchIds,
                    searchInput,
                    isAdmin
                );

                if (!employees.Any())
                {
                    return Json(new { error = "No se encontraron empleados activos con los criterios especificados." });
                }

                var result = employees.Select(employee => new
                {
                    employeeName = employee.NombreCompleto ?? $"{employee.PrimerNombre} {employee.PrimerApellido}",
                    firstName = employee.PrimerNombre ?? "",
                    middleName = employee.SegundoNombre ?? "",
                    lastName = employee.PrimerApellido ?? "",
                    secondLastName = employee.SegundoApellido ?? "",
                    employeeId = employee.CodCedula,
                    cargoId = employee.CodCargo,
                    cargoName = employee.Cargo?.NombreCargo ?? "",
                    unitId = employee.Cargo?.Unidad?.CodUnidad ?? "",
                    unitName = employee.Cargo?.Unidad?.NombreUnidad ?? "",
                    branchId = employee.CodSucursal,
                    branchName = employee.Sucursal?.NombreSucursal ?? "",
                    unitType = employee.Cargo?.Unidad?.TipoUnidad ?? "",
                    photoUrl = employee.FotoUrl != null ? $"assets/profile-img/{Path.GetFileName(employee.FotoUrl)}" : ""
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CONTROLADOR: Error interno en GetEmployeeInfo para usuario {UserId}. Mensaje: {ErrorMessage}", currentUser.Id, ex.Message);
                return Json(new { error = $"Error interno del servidor: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene el estado actual de registro de un empleado (entrada abierta, salida completada, etc.).
        /// </summary>
        /// <param name="employeeId">La cédula del empleado.</param>
        /// <returns>Un objeto EmployeeLogStateDto que describe el estado del log.</returns>
        [HttpGet("GetEmployeeStatus")]
        public async Task<IActionResult> GetEmployeeLogStatus(int employeeId)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            try
            {
                var statusDto = await _employeeLogService.GetEmployeeLogStatusAsync(employeeId);
                return Json(statusDto);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CONTROLLER: Error in GetEmployeeLogStatus for employee {EmployeeId} by user {UserId}. Message: {ErrorMessage}", employeeId, currentUser.Id, ex.Message);
                return Json(new { status = "error", error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Registra una nueva entrada de empleado o una entrada/salida combinada.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Create' para la vista 'REG'.
        /// Si se detectan condiciones que requieren confirmación (ej. horas trabajadas inusuales), se devuelve una solicitud de confirmación.
        /// </remarks>
        /// <param name="logDto">Objeto EmployeeLogDto con los datos del registro.</param>
        /// <param name="confirmedValidation">Parámetro opcional para indicar que una validación previa ha sido confirmada por el usuario (ej. "minHours").</param>
        /// <returns>Un ServiceResult indicando el éxito, fallo o necesidad de confirmación.</returns>
        [HttpPost("RecordEntryExit")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "REG")] // Assuming "REG" for create permission
        public async Task<IActionResult> RecordEntryExit([FromBody] EmployeeLogDto logDto, string? confirmedValidation = null)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            var result = await _employeeLogService.RecordEmployeeEntryExitAsync(logDto, currentUser.Id, confirmedValidation);

            if (result.Success)
            {
                Log.Information("| User: {User} | IP: {Ip} | Action: Employee Log Recorded | EmployeeId: {EmployeeId} | Result: Success |", currentUser.UserName, ViewBag.Ip, logDto.EmployeeId);
            }
            else
            {
                Log.Warning("| User: {User} | IP: {Ip} | Action: Employee Log Failed | EmployeeId: {EmployeeId} | Result: {Message} |", currentUser.UserName, ViewBag.Ip, logDto.EmployeeId, result.Message);
            }
            return Json(result);
        }

        /// <summary>
        /// Muestra el formulario para editar un registro de log de empleado existente.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Edit' para la vista 'REG'.
        /// </remarks>
        /// <param name="id">El ID del registro de log a editar.</param>
        /// <returns>La vista del formulario de edición con los datos del log.</returns>
        [HttpGet("EditLog/{id}")]
        [RequiredPermission(PermissionType.Edit, "REG")]
        public async Task<IActionResult> EditLog(int id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeLogAsync(currentUser, "Edit Employee Log");

            var logEntry = await _context.SegRegistroEmpleados
                                         .Include(r => r.Empleado)
                                             .ThenInclude(e => e.Cargo)
                                                 .ThenInclude(c => c.Unidad)
                                         .Include(r => r.Sucursal)
                                         .FirstOrDefaultAsync(r => r.Id == id);

            if (logEntry == null)
            {
                Log.Warning("| User: {User} | IP: {Ip} | Action: Accessing Edit Log Form | LogId: {LogId} | Result: Not Found |", currentUser.UserName, ViewBag.Ip, id);
                TempData["ErrorMessage"] = "Log entry not found.";
                return RedirectToAction(nameof(EmployeeLogHistory));
            }

            // Check branch permission (similar to other modules)
            bool isAdmin = (bool)ViewBag.IsAdmin;
            if (!isAdmin)
            {
                List<int> permittedBranchIds = await GetUserPermittedSucursalesAsync(currentUser.Id);
                if (!permittedBranchIds.Contains(logEntry.CodSucursal))
                {
                    Log.Warning("| User: {User} | IP: {Ip} | Action: Accessing Edit Log Form | LogId: {LogId} | Result: Forbidden (Branch access denied) |", currentUser.UserName, ViewBag.Ip, id);
                    return Forbid();
                }
            }

            Log.Information("| User: {User} | IP: {Ip} | Action: Accessing Edit Log Form | LogId: {LogId} | Result: Access Granted |", currentUser.UserName, ViewBag.Ip, id);
            return View(logEntry); // Pass the log entry to the view
        }

        /// <summary>
        /// Actualiza un registro de log de empleado existente, utilizado para registrar salidas o modificar detalles.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Create' para la vista 'REG' (para completar el registro).
        /// </remarks>
        /// <param name="id">El ID del registro de log a actualizar.</param>
        /// <param name="logDto">Objeto EmployeeLogDto con los datos actualizados.</param>
        /// <param name="confirmedValidation">Tipo de validación confirmada, si aplica.</param>
        /// <returns>Un ServiceResult indicando el éxito o fallo de la actualización.</returns>
        [HttpPost("UpdateLog/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "REG")]
        public async Task<IActionResult> UpdateLog([FromRoute] int id, [FromBody] EmployeeLogDto logDto, string? confirmedValidation)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            var result = await _employeeLogService.UpdateEmployeeLogAsync(id, logDto, currentUser.Id, confirmedValidation);

            if (result.Success)
            {
                Log.Information("| User: {User} | IP: {Ip} | Action: Employee Log Updated | LogId: {LogId} | EmployeeId: {EmployeeId} | Result: Success |", currentUser.UserName, ViewBag.Ip, id, logDto.EmployeeId);
            }
            else
            {
                Log.Warning("| User: {User} | IP: {Ip} | Action: Employee Log Update Failed | LogId: {LogId} | EmployeeId: {EmployeeId} | Result: {Message} |", currentUser.UserName, ViewBag.Ip, id, logDto.EmployeeId, result.Message);
            }
            return Json(result);
        }

        /// <summary>
        /// Muestra el formulario para registrar la salida manual de un vehículo asociado a un log.
        /// </summary>
        /// <param name="id">El ID del registro de log.</param>
        /// <returns>La vista del formulario de salida manual.</returns>
        [HttpGet("RecordManualExit/{id}")]
        [RequiredPermission(PermissionType.Create, "REG")]
        public async Task<IActionResult> RecordManualExit(int id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeLogAsync(currentUser, "Manual Exit");

            var logEntry = await _context.SegRegistroEmpleados
                                         .Include(r => r.Empleado)
                                         .FirstOrDefaultAsync(r => r.Id == id);

            if (logEntry == null)
            {
                Log.Warning("| User: {User} | IP: {Ip} | Action: Accessing Manual Exit Form | LogId: {LogId} | Result: Not Found |", currentUser.UserName, ViewBag.Ip, id);
                TempData["ErrorMessage"] = "Log entry not found.";
                return RedirectToAction(nameof(EmployeeLogHistory));
            }

            bool isAdmin = (bool)ViewBag.IsAdmin;
            if (!isAdmin)
            {
                List<int> permittedBranchIds = await GetUserPermittedSucursalesAsync(currentUser.Id);
                if (!permittedBranchIds.Contains(logEntry.CodSucursal))
                {
                    Log.Warning("| User: {User} | IP: {Ip} | Action: Accessing Manual Exit Form | LogId: {LogId} | Result: Forbidden (Branch access denied) |", currentUser.UserName, ViewBag.Ip, id);
                    return Forbid();
                }
            }

            logEntry.FechaSalida = DateOnly.FromDateTime(DateTime.Now.Date);
            logEntry.HoraSalida = TimeOnly.FromDateTime(DateTime.Now);

            Log.Information("| User: {User} | IP: {Ip} | Action: Accessing Manual Exit Form | LogId: {LogId} | Result: Access Granted |", currentUser.UserName, ViewBag.Ip, id);
            return View(logEntry); // Pass the log entry to the view
        }

        /// <summary>
        /// Registra la salida manual de un vehículo.
        /// </summary>
        /// <param name="id">El ID del registro de log a actualizar.</param>
        /// <param name="logDto">Objeto EmployeeLogDto con la fecha y hora de salida.</param>
        /// <returns>Un ServiceResult indicando el éxito o fallo.</returns>
        [HttpPost("RecordManualExit/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "REG")]
        public async Task<IActionResult> RecordManualExit([FromRoute] int id, [FromBody] EmployeeLogDto logDto)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(logDto.ExitDate) || string.IsNullOrWhiteSpace(logDto.ExitTime))
            {
                return Json(ServiceResult.FailureResult("La fecha y hora de salida son obligatorias para el registro manual."));
            }

            DateOnly parsedExitDate;
            TimeOnly parsedExitTime;

            if (!DateOnly.TryParse(logDto.ExitDate, out parsedExitDate))
            {
                return Json(ServiceResult.FailureResult("El formato de la fecha de salida es inválido."));
            }
            if (!TimeOnly.TryParse(logDto.ExitTime, out parsedExitTime))
            {
                return Json(ServiceResult.FailureResult("El formato de la hora de salida es inválido."));
            }

            var result = await _employeeLogService.RecordManualEmployeeExitAsync(
                id,
                parsedExitDate,
                parsedExitTime,
                currentUser.Id,
                logDto.ConfirmedValidation
            );


            if (result.Success)
            {
                Log.Information("| User: {User} | IP: {Ip} | Action: Manual Exit Recorded | LogId: {LogId} | Result: Success |", currentUser.UserName, ViewBag.Ip, id);
            }
            else
            {
                Log.Warning("| User: {User} | IP: {Ip} | Action: Manual Exit Failed | LogId: {LogId} | Result: {Message} |", currentUser.UserName, ViewBag.Ip, id, result.Message);
            }
            return Json(result);
        }

        /// <summary>
        /// Muestra el registros de empleados entre un dia actual y un dia anterior al actual, con opciones de filtrado y paginación.
        /// </summary>
        /// <param name="cargoId">Filtro por ID de cargo.</param>
        /// <param name="unitId">Filtro por ID de unidad.</param>
        /// <param name="branchId">Filtro por ID de sucursal.</param>
        /// <param name="startDate">Fecha de inicio para el filtro de rango.</param>
        /// <param name="endDate">Fecha de fin para el filtro de rango.</param>
        /// <param name="logStatus">Estado del log (0: abierto, 1: cerrado).</param>
        /// <param name="searchTerm">Término de búsqueda por cédula o nombre.</param>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Número de elementos por página.</param>
        /// <returns>La vista de logs del registro de empleados, con datos paginados.</returns>
        [HttpGet("EmployeeLog")]
        [RequiredPermission(PermissionType.View, "REG")]
        public async Task<IActionResult> EmployeeLog(int? cargoId, string? unitId, int? branchId, DateOnly? startDate, DateOnly? endDate, int? logStatus, string? search, int page = 1, int pageSize = 15)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
            DateOnly? filterStartDate = startDate ?? yesterday;
            DateOnly? filterEndDate = endDate ?? today;

            await SetCommonViewBagsEmployeeLogAsync(currentUser, "Registro de Empleados");

            bool isAdmin = (bool)ViewBag.IsAdmin;

            var (data, totalData) = await _employeeLogService.GetFilteredEmployeeLogsAsync(
                currentUser.Id,
                cargoId,
                unitId,
                branchId,
                filterStartDate,
                filterEndDate,
                logStatus,
                search,
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
            ViewBag.CurrentUnitId = unitId;
            ViewBag.CurrentBranchId = branchId;
            ViewBag.CurrentStartDate = filterStartDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentEndDate = filterEndDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentLogStatus = logStatus;

            Log.Information("| User: {User} | IP: {Ip} | Action: Accessing Employee Log History | Count: {Count} | Result: Access Granted |", currentUser.UserName, ViewBag.Ip, data.Count);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("~/Views/EmployeeLog/_MainLogTablePartial.cshtml", data);
            }

            return View(data);
        }

        /// <summary>
        /// Muestra el historial de registros de empleados, con opciones de filtrado y paginación.
        /// </summary>
        /// <param name="cargoId">Filtro por ID de cargo.</param>
        /// <param name="unitId">Filtro por ID de unidad.</param>
        /// <param name="branchId">Filtro por ID de sucursal.</param>
        /// <param name="startDate">Fecha de inicio para el filtro de rango.</param>
        /// <param name="endDate">Fecha de fin para el filtro de rango.</param>
        /// <param name="logStatus">Estado del log (0: abierto, 1: cerrado).</param>
        /// <param name="searchTerm">Término de búsqueda por cédula o nombre.</param>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Número de elementos por página.</param>
        /// <returns>La vista del historial de logs de empleados, con datos paginados.</returns>
        [HttpGet("EmployeeLogHistory")]
        [RequiredPermission(PermissionType.View, "REGHIS")]
        public async Task<IActionResult> EmployeeLogHistory(int? cargoId, string? unitId, int? branchId, DateOnly? startDate, DateOnly? endDate, int? logStatus, string? search, int page = 1, int pageSize = 15)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            Log.Information($"Usuario autenticado: {User.Identity.Name}");

            await SetCommonViewBagsEmployeeLogAsync(currentUser, "Historial");

            bool isAdmin = (bool)ViewBag.IsAdmin;

            var (data, totalData) = await _employeeLogService.GetFilteredEmployeeLogsAsync(
                currentUser.Id,
                cargoId,
                unitId,
                branchId,
                startDate,
                endDate,
                logStatus,
                search,
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
            ViewBag.CurrentUnitId = unitId;
            ViewBag.CurrentBranchId = branchId;
            ViewBag.CurrentStartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentEndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentLogStatus = logStatus;

            Log.Information("| User: {User} | IP: {Ip} | Action: Accessing Employee Log History | Count: {Count} | Result: Access Granted |", currentUser.UserName, ViewBag.Ip, data.Count);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("~/Views/EmployeeLog/_EmployeeLogTablePartial.cshtml", data);
            }

            return View(data);
        }

        /// <summary>
        /// Muestra los detalles de un registro de log de empleado específico.
        /// </summary>
        /// <param name="id">El ID del registro de log a mostrar.</param>
        /// <returns>La vista de detalles del log.</returns>
        [HttpGet("LogDetails/{id}")]
        [RequiredPermission(PermissionType.View, "REG")]
        public async Task<IActionResult> LogDetails(int id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeLogAsync(currentUser, "Log Details");

            var logEntry = await _context.SegRegistroEmpleados
                                         .Include(r => r.Empleado)
                                             .ThenInclude(e => e.Cargo)
                                                 .ThenInclude(c => c.Unidad)
                                         .Include(r => r.Sucursal)
                                         .Include(r => r.UsuarioRegistro)
                                         .FirstOrDefaultAsync(r => r.Id == id);

            if (logEntry == null)
            {
                Log.Warning("| User: {User} | IP: {Ip} | Action: Accessing Log Details | LogId: {LogId} | Result: Not Found |", currentUser.UserName, ViewBag.Ip, id);
                TempData["ErrorMessage"] = "Log entry not found.";
                return RedirectToAction(nameof(EmployeeLogHistory));
            }

            // Check branch permission (similar to other modules)
            bool isAdmin = (bool)ViewBag.IsAdmin;
            if (!isAdmin)
            {
                List<int> permittedBranchIds = await GetUserPermittedSucursalesAsync(currentUser.Id);
                if (!permittedBranchIds.Contains(logEntry.CodSucursal))
                {
                    Log.Warning("| User: {User} | IP: {Ip} | Action: Accessing Log Details | LogId: {LogId} | Result: Forbidden (Branch access denied) |", currentUser.UserName, ViewBag.Ip, id);
                    return Forbid();
                }
            }

            Log.Information("| User: {User} | IP: {Ip} | Action: Accessing Log Details | LogId: {LogId} | Result: Access Granted |", currentUser.UserName, ViewBag.Ip, id);
            return View("LogDetails", logEntry); // Pass the log entry to the view
        }
    }
}