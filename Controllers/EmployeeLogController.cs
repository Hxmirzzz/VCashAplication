using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VCashApp.Data;
using VCashApp.Filters;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.EmployeeLog;
using VCashApp.Services;
using VCashApp.Services.DTOs;
using VCashApp.Services.EmployeeLog.Application;
using VCashApp.Services.EmployeeLog.Queries;
using VCashApp.Services.Helpers;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Gestión de registros de entrada/salida (Employee Log).
    /// - Usa IBranchContext para filtrar/validar sucursal activa y sucursales permitidas
    /// - No usa procedimientos almacenados desde el controlador
    /// - Delegación de negocio al IEmployeeLogService (EF puro)
    /// </summary>
    [ApiController]
    [Route("/EmployeeLog")]
    public class EmployeeLogController : BaseController
    {
        private readonly IEmployeeLogService _employeeLogService;
        private readonly IBranchContext _branchContext;
        private readonly IEmployeeLogLookupsService _lookups;

        public EmployeeLogController(
            IEmployeeLogService employeeLogService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IBranchContext branchContext,
            IEmployeeLogLookupsService lookups
        ) : base(context, userManager)
        {
            _employeeLogService = employeeLogService;
            _branchContext = branchContext;
            _lookups = lookups;
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------
        /// <summary>
        /// Sucursales, estados y permisos por vista (patrón CEF).
        /// No arma listas aquí; delega a LookupsService.
        /// </summary>
        private async Task SetCommonViewBagsEmployeeLogAsync(ApplicationUser currentUser, string pageName, params string[] codVistas)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);
            bool isAdmin = ViewBag.IsAdmin;

            var dds = await _lookups.GetDropdownListsAsync(currentUser.Id, isAdmin);
            ViewBag.AvailableBranches = dds.Branches;
            ViewBag.Cargos = dds.Cargos;
            ViewBag.Unidades = dds.Unidades;
            ViewBag.LogStatusFilterList = dds.LogStatuses;

            // Códigos de vista por defecto para EmployeeLog
            var vistas = (codVistas != null && codVistas.Length > 0)
                ? codVistas
                : new[] { "REG", "REGHIS" };

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            async Task<bool> HasAsync(PermissionType p)
            {
                foreach (var v in vistas)
                {
                    if (await HasPermisionForView(userRoles, v, p))
                        return true;
                }
                return false;
            }

            ViewBag.HasCreate = await HasAsync(PermissionType.Create);
            ViewBag.HasEdit = await HasAsync(PermissionType.Edit);
            ViewBag.HasView = await HasAsync(PermissionType.View);
            ViewBag.CanCreateLog = ViewBag.HasCreate;
            ViewBag.CanEditLog = ViewBag.HasEdit;
            ViewBag.CanViewHistory = ViewBag.HasView;
        }

        private async Task<List<int>> GetUserAssignedBranchIdsAsync(string userId)
        {
            var ids = await _context.UserClaims
                .Where(uc => uc.UserId == userId && uc.ClaimType == Infrastructure.Branches.BranchClaimTypes.AssignedBranch)
                .Select(uc => uc.ClaimValue)
                .ToListAsync();

            return ids
                .Select(v => int.TryParse(v, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .Distinct()
                .ToList();
        }

        private async Task<(bool isAdmin, List<int> permittedBranchIds)> ResolveBranchScopeAsync(ApplicationUser user)
        {
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                // Admin ve todas activas, pero si hay CurrentBranchId se usa como filtro por UX
                var allActive = await _context.AdmSucursales
                    .AsNoTracking()
                    .Where(s => s.Estado)
                    .Select(s => s.CodSucursal)
                    .ToListAsync();
                return (true, allActive);
            }

            // No Admin -> sucursales asignadas (y opcionalmente filtra por CurrentBranchId)
            var assigned = await GetUserAssignedBranchIdsAsync(user.Id);
            if (_branchContext.CurrentBranchId.HasValue)
            {
                var only = _branchContext.CurrentBranchId.Value;
                if (assigned.Contains(only)) return (false, new List<int> { only });
                // Si no es dueña de esa sucursal, no puede ver nada.
                return (false, new List<int>());
            }

            // Todas mis sucursales (chip) -> assigned
            return (false, assigned);
        }

        private bool EnforceBranchAccessForEntity(SegRegistroEmpleado log, List<int> permittedBranchIds, bool isAdmin)
        {
            if (isAdmin) return true;
            return permittedBranchIds.Contains(log.CodSucursal);
        }

        // --------------------------------------------------------------------
        // UI: Crear / LogEntry
        // --------------------------------------------------------------------
        [HttpGet("LogEntry")]
        [RequiredPermission(PermissionType.Create, "REG")]
        public async Task<IActionResult> LogEntry()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeLogAsync(user, "Crear");

            var vm = await _employeeLogService.GetEntryViewModelAsync(
                user.UserName ?? "",
                ViewBag.UnidadName,
                ViewBag.BranchName,
                ViewBag.FullName,
                ViewBag.CanCreateLog ?? false,
                ViewBag.CanEditLog ?? false
            );

            Log.Information("| User: {User} | IP: {Ip} | Action: Access LogEntry | OK |", user.UserName, ViewBag.Ip);
            return View(vm);
        }

        // --------------------------------------------------------------------
        // API: Búsqueda de empleados (autocomplete)
        // --------------------------------------------------------------------
        [HttpGet("GetEmployeeInfo")]
        public async Task<IActionResult> GetEmployeeInfo(string searchInput)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return Unauthorized();

            try
            {
                if (string.IsNullOrWhiteSpace(searchInput))
                    return Json(new { error = "El campo de búsqueda no puede estar vacío." });

                var (isAdmin, permitted) = await ResolveBranchScopeAsync(user);
                if (!isAdmin && !permitted.Any())
                    return Json(new { error = "No tiene sucursales asignadas para buscar empleados." });

                var employees = await _employeeLogService.GetEmployeeInfoAsync(
                    user.Id, permitted, searchInput, isAdmin);

                if (!employees.Any())
                    return Json(new { error = "No se encontraron empleados activos con los criterios especificados." });

                var result = employees.Select(e => new
                {
                    employeeName = e.NombreCompleto ?? $"{e.PrimerNombre} {e.PrimerApellido}",
                    firstName = e.PrimerNombre ?? "",
                    middleName = e.SegundoNombre ?? "",
                    lastName = e.PrimerApellido ?? "",
                    secondLastName = e.SegundoApellido ?? "",
                    employeeId = e.CodCedula,
                    cargoId = e.CodCargo,
                    cargoName = e.Cargo?.NombreCargo ?? "",
                    unitId = e.Cargo?.Unidad?.CodUnidad ?? "",
                    unitName = e.Cargo?.Unidad?.NombreUnidad ?? "",
                    branchId = e.CodSucursal,
                    branchName = e.Sucursal?.NombreSucursal ?? "",
                    unitType = e.Cargo?.Unidad?.TipoUnidad ?? "",
                    photoUrl = e.FotoUrl != null ? $"assets/profile-img/{Path.GetFileName(e.FotoUrl)}" : ""
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetEmployeeInfo error user {UserId}", user.Id);
                return Json(new { error = $"Error interno del servidor: {ex.Message}" });
            }
        }

        // --------------------------------------------------------------------
        // API: Estado del empleado (abierto/cerrado/hoy/ayer)
        // --------------------------------------------------------------------
        [HttpGet("GetEmployeeStatus")]
        public async Task<IActionResult> GetEmployeeLogStatus(int employeeId)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return Unauthorized();

            try
            {
                var statusDto = await _employeeLogService.GetEmployeeLogStatusAsync(employeeId);
                return Json(statusDto);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetEmployeeLogStatus error for {EmployeeId} by {UserId}", employeeId, user.Id);
                return Json(new { status = "error", error = $"Internal server error: {ex.Message}" });
            }
        }

        // --------------------------------------------------------------------
        // API: Crear entrada o combinado
        // --------------------------------------------------------------------
        [HttpPost("RecordEntryExit")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "REG")]
        public async Task<IActionResult> RecordEntryExit([FromBody] EmployeeLogDto logDto, string? confirmedValidation = null)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return Unauthorized();

            var result = await _employeeLogService.RecordEmployeeEntryExitAsync(logDto, user.Id, confirmedValidation);
            if (result.Success)
                Log.Information("| User: {User} | IP: {Ip} | RecordEntryExit OK | Emp:{Emp} |", user.UserName, ViewBag.Ip, logDto.EmployeeId);
            else
                Log.Warning("| User: {User} | RecordEntryExit FAIL | Emp:{Emp} | {Msg}", user.UserName, logDto.EmployeeId, result.Message);

            return Json(result);
        }

        // --------------------------------------------------------------------
        // UI: Editar log
        // --------------------------------------------------------------------
        [HttpGet("EditLog/{id}")]
        [RequiredPermission(PermissionType.Edit, "REG")]
        public async Task<IActionResult> EditLog(int id)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeLogAsync(user, "Editar");

            var entity = await _context.SegRegistroEmpleados
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(r => r.Sucursal)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null)
            {
                TempData["ErrorMessage"] = "Log entry not found.";
                return RedirectToAction(nameof(EmployeeLogHistory));
            }

            var (isAdmin, permitted) = await ResolveBranchScopeAsync(user);
            if (!EnforceBranchAccessForEntity(entity, permitted, isAdmin))
                return Forbid();

            var vm = await _employeeLogService.GetEditViewModelAsync(id, ViewBag.CanEditLog ?? false);
            if (vm == null)
            {
                TempData["ErrorMessage"] = "Log entry not found.";
                return RedirectToAction(nameof(EmployeeLogHistory));
            }

            // Prefills HTML
            ViewBag.EntryDateVal = TimeFormatHelper.ToDateInput(entity.FechaEntrada);
            ViewBag.EntryTimeVal = TimeFormatHelper.ToTimeInput24(entity.HoraEntrada);
            ViewBag.ExitDateVal = TimeFormatHelper.ToDateInput(entity.FechaSalida);
            ViewBag.ExitTimeVal = TimeFormatHelper.ToTimeInput24(entity.HoraSalida);

            return View(vm);
        }

        // --------------------------------------------------------------------
        // API: Actualizar log (salida o corrección)
        // --------------------------------------------------------------------
        [HttpPost("UpdateLog/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "REG")]
        public async Task<IActionResult> UpdateLog([FromRoute] int id, [FromBody] EmployeeLogDto logDto, string? confirmedValidation)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return Unauthorized();

            // Protección extra por sucursal antes de delegar
            var entity = await _context.SegRegistroEmpleados.FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null) return Json(ServiceResult.FailureResult("Registro no encontrado."));

            var (isAdmin, permitted) = await ResolveBranchScopeAsync(user);
            if (!EnforceBranchAccessForEntity(entity, permitted, isAdmin))
                return Forbid();

            var result = await _employeeLogService.UpdateEmployeeLogAsync(id, logDto, user.Id, confirmedValidation);
            if (result.Success)
                Log.Information("| User:{User} | UpdateLog OK | Id:{Id}", user.UserName, id);
            else
                Log.Warning("| User:{User} | UpdateLog FAIL | Id:{Id} | {Msg}", user.UserName, id, result.Message);

            return Json(result);
        }

        // --------------------------------------------------------------------
        // UI: Salida manual
        // --------------------------------------------------------------------
        [HttpGet("RecordManualExit/{id}")]
        [RequiredPermission(PermissionType.Create, "REG")]
        public async Task<IActionResult> RecordManualExit(int id)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeLogAsync(user, "Salida Manual");

            var entity = await _context.SegRegistroEmpleados.Include(r => r.Empleado)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null)
            {
                TempData["ErrorMessage"] = "Log entry not found.";
                return RedirectToAction(nameof(EmployeeLogHistory));
            }

            var (isAdmin, permitted) = await ResolveBranchScopeAsync(user);
            if (!EnforceBranchAccessForEntity(entity, permitted, isAdmin))
                return Forbid();

            // Propuesta por defecto
            entity.FechaSalida = DateOnly.FromDateTime(DateTime.Now.Date);
            entity.HoraSalida = TimeOnly.FromDateTime(DateTime.Now);

            ViewBag.EntryDateVal = TimeFormatHelper.ToDateInput(entity.FechaEntrada);
            ViewBag.EntryTimeVal = TimeFormatHelper.ToTimeInput24(entity.HoraEntrada);
            ViewBag.ExitDateVal = TimeFormatHelper.ToDateInput(entity.FechaSalida);
            ViewBag.ExitTimeVal = TimeFormatHelper.ToTimeInput24(entity.HoraSalida);

            var vm = await _employeeLogService.GetManualExitViewModelAsync(
                id, ViewBag.CanCreateLog ?? false, ViewBag.CanEditLog ?? false);

            if (vm == null)
            {
                TempData["ErrorMessage"] = "Log entry not found.";
                return RedirectToAction(nameof(EmployeeLogHistory));
            }

            return View(vm);
        }

        [HttpPost("RecordManualExit/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "REG")]
        public async Task<IActionResult> RecordManualExit([FromRoute] int id, [FromBody] EmployeeLogDto logDto)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(logDto.ExitDate) || string.IsNullOrWhiteSpace(logDto.ExitTime))
                return Json(ServiceResult.FailureResult("La fecha y hora de salida son obligatorias."));

            if (!DateOnly.TryParse(logDto.ExitDate, out var exitDate))
                return Json(ServiceResult.FailureResult("Formato de fecha de salida inválido."));
            if (!TimeOnly.TryParse(logDto.ExitTime, out var exitTime))
                return Json(ServiceResult.FailureResult("Formato de hora de salida inválido."));

            // Chequeo de sucursal
            var entity = await _context.SegRegistroEmpleados.FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null) return Json(ServiceResult.FailureResult("Registro no encontrado."));

            var (isAdmin, permitted) = await ResolveBranchScopeAsync(user);
            if (!EnforceBranchAccessForEntity(entity, permitted, isAdmin))
                return Forbid();

            var result = await _employeeLogService.RecordManualEmployeeExitAsync(
                id, exitDate, exitTime, user.Id, logDto.ConfirmedValidation);

            return Json(result);
        }

        // --------------------------------------------------------------------
        // UI: Dashboard (Hoy / Ayer por defecto)
        // --------------------------------------------------------------------
        [HttpGet("EmployeeLog")]
        [RequiredPermission(PermissionType.View, "REG")]
        public async Task<IActionResult> EmployeeLog(
            int? cargoId, string? unitId, int? branchId,
            DateOnly? startDate, DateOnly? endDate, int? logStatus,
            string? search, int page = 1, int pageSize = 15)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

            var filterStart = startDate ?? yesterday;
            var filterEnd = endDate ?? today;

            await SetCommonViewBagsEmployeeLogAsync(user, "Registro de Empleados");

            // Si hay sucursal activa en chip y no viene branchId, la imponemos para coherencia visual
            if (!_branchContext.AllBranches && _branchContext.CurrentBranchId.HasValue && branchId is null)
                branchId = _branchContext.CurrentBranchId.Value;

            var isAdmin = (bool)ViewBag.IsAdmin;
            var (rows, total) = await _employeeLogService.GetFilteredEmployeeLogsAsync(
                user.Id, cargoId, string.IsNullOrWhiteSpace(unitId) ? null : unitId, branchId,
                filterStart, filterEnd, logStatus, string.IsNullOrWhiteSpace(search) ? null : search,
                page, pageSize, isAdmin);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.TotalData = total;
            ViewBag.PageSize = pageSize;

            var vm = new EmployeeLogDashboardViewModel
            {
                SearchTerm = search,
                CargoId = cargoId,
                UnitId = unitId,
                BranchId = branchId,
                StartDate = filterStart,
                EndDate = filterEnd,
                LogStatus = logStatus,
                CurrentPage = page,
                PageSize = pageSize,
                TotalData = total,
                TotalPages = ViewBag.TotalPages,
                Logs = rows,
                Cargos = ViewBag.Cargos,
                Unidades = ViewBag.Unidades,
                Sucursales = ViewBag.Sucursales,
                LogStatusList = ViewBag.LogStatusFilterList,
                CanCreateLog = ViewBag.CanCreateLog ?? false,
                CanEditLog = ViewBag.CanEditLog ?? false,
                CanViewHistory = ViewBag.CanViewHistory ?? false
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("~/Views/EmployeeLog/_MainLogTablePartial.cshtml", vm.Logs);

            return View(vm);
        }

        // --------------------------------------------------------------------
        // UI: Historial
        // --------------------------------------------------------------------
        [HttpGet("EmployeeLogHistory")]
        [RequiredPermission(PermissionType.View, "REGHIS")]
        public async Task<IActionResult> EmployeeLogHistory(
            int? cargoId, string? unitId, int? branchId,
            DateOnly? startDate, DateOnly? endDate, int? logStatus,
            string? search, int page = 1, int pageSize = 15)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeLogAsync(user, "Historial");

            // Si hay sucursal activa en chip y no viene branchId, la imponemos
            if (!_branchContext.AllBranches && _branchContext.CurrentBranchId.HasValue && branchId is null)
                branchId = _branchContext.CurrentBranchId.Value;

            var isAdmin = (bool)ViewBag.IsAdmin;

            var (rows, total) = await _employeeLogService.GetFilteredEmployeeLogsAsync(
                user.Id, cargoId, string.IsNullOrWhiteSpace(unitId) ? null : unitId, branchId,
                startDate, endDate, logStatus, string.IsNullOrWhiteSpace(search) ? null : search,
                page, pageSize, isAdmin);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.TotalData = total;
            ViewBag.PageSize = pageSize;

            var vm = new EmployeeLogDashboardViewModel
            {
                SearchTerm = search,
                CargoId = cargoId,
                UnitId = unitId,
                BranchId = branchId,
                StartDate = startDate,
                EndDate = endDate,
                LogStatus = logStatus,
                CurrentPage = page,
                PageSize = pageSize,
                TotalData = total,
                TotalPages = ViewBag.TotalPages,
                Logs = rows,
                Cargos = ViewBag.Cargos,
                Unidades = ViewBag.Unidades,
                Sucursales = ViewBag.Sucursales,
                LogStatusList = ViewBag.LogStatusFilterList,
                CanCreateLog = ViewBag.CanCreateLog ?? false,
                CanEditLog = ViewBag.CanEditLog ?? false,
                CanViewHistory = ViewBag.CanViewHistory ?? false
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("~/Views/EmployeeLog/_EmployeeLogTablePartial.cshtml", rows);

            return View(vm);
        }

        // --------------------------------------------------------------------
        // UI: Detalle
        // --------------------------------------------------------------------
        [HttpGet("LogDetails/{id}")]
        [RequiredPermission(PermissionType.View, "REG")]
        public async Task<IActionResult> LogDetails(int id)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeLogAsync(user, "Detalles");

            var entity = await _context.SegRegistroEmpleados
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(r => r.Sucursal)
                .Include(r => r.UsuarioRegistro)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null)
            {
                TempData["ErrorMessage"] = "Log entry not found.";
                return RedirectToAction(nameof(EmployeeLogHistory));
            }

            var (isAdmin, permitted) = await ResolveBranchScopeAsync(user);
            if (!EnforceBranchAccessForEntity(entity, permitted, isAdmin))
                return Forbid();

            var vm = await _employeeLogService.GetDetailsViewModelAsync(id);
            if (vm == null)
            {
                TempData["ErrorMessage"] = "Log entry not found.";
                return RedirectToAction(nameof(EmployeeLogHistory));
            }

            ViewBag.EntryDateVal = TimeFormatHelper.ToDateInput(entity.FechaEntrada);
            ViewBag.EntryTimeVal = TimeFormatHelper.ToTimeInput24(entity.HoraEntrada);
            ViewBag.ExitDateVal = TimeFormatHelper.ToDateInput(entity.FechaSalida);
            ViewBag.ExitTimeVal = TimeFormatHelper.ToTimeInput24(entity.HoraSalida);

            return View("LogDetails", vm);
        }
    }
}