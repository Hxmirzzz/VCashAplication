using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models;
using VCashApp.Models.Dtos.EmployeeLog;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.EmployeeLog;
using VCashApp.Services.DTOs;
using VCashApp.Services.EmployeeLog.Integration;
using VCashApp.Services.EmployeeLog.Mapping;
using VCashApp.Services.EmployeeLog.Queries;
using VCashApp.Services.EmployeeLog.Validation;

namespace VCashApp.Services.EmployeeLog.Application
{
    /// <summary>
    /// Servicio de negocio para Registro de Empleados (Entrada/Salida) sin SPs.
    /// Orquesta preparación de VMs, guardado/actualización y consultas usando EF Core + LINQ.
    /// </summary>
    public class EmployeeLogService : IEmployeeLogService
    {
        private readonly AppDbContext _context;
        private readonly IDailyRouteUpdater _dailyRoute;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBranchContext _branch;

        public EmployeeLogService(
            AppDbContext context,
            IDailyRouteUpdater dailyRoute,
            UserManager<ApplicationUser> userManager,
            IBranchContext branch)
        {
            _context = context;
            _dailyRoute = dailyRoute;
            _userManager = userManager;
            _branch = branch;
        }

        // ---------------------------------------------------------------------
        //  PREPARACIÓN DE PÁGINAS / VIEWMODELS
        // ---------------------------------------------------------------------

        public Task<EmployeeLogEntryViewModel> GetEntryViewModelAsync(
            string userName, string? unidadName, string? branchName, string? fullName,
            bool canCreate, bool canEdit)
        {
            var vm = new EmployeeLogEntryViewModel
            {
                UserName = userName,
                PageName = "Crear",
                UnidadName = unidadName,
                BranchName = branchName,
                FullName = fullName,
                CanCreateLog = canCreate,
                CanEditLog = canEdit
            };
            return Task.FromResult(vm);
        }

        public async Task<EmployeeLogEditViewModel?> GetEditViewModelAsync(int id, bool canEditLog)
        {
            var s = await _context.SegRegistroEmpleados.AsNoTracking()
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(r => r.Sucursal)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (s == null) return null;

            return EmployeeLogMappers.ToEditVm(s, canEditLog);
        }

        public async Task<EmployeeLogDetailsViewModel?> GetDetailsViewModelAsync(int id)
        {
            var s = await _context.SegRegistroEmpleados.AsNoTracking()
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(r => r.Sucursal)
                .FirstOrDefaultAsync(r => r.Id == id);

            return s == null ? null : EmployeeLogMappers.ToDetailsVm(s);
        }

        public async Task<EmployeeLogManualExitViewModel?> GetManualExitViewModelAsync(
            int id, bool canCreateLog, bool canEditLog)
        {
            var s = await _context.SegRegistroEmpleados.AsNoTracking()
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(r => r.Sucursal)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (s == null) return null;

            return EmployeeLogMappers.ToManualExitVm(s, canCreateLog, canEditLog);
        }

        public async Task<EmployeeLogStatusDto> GetEmployeeLogStatusAsync(int employeeId)
        {
            var now = DateTime.Now;
            var currentDate = DateOnly.FromDateTime(now.Date);
            var currentTime = TimeOnly.FromDateTime(now);

            var employee = await _context.AdmEmpleados
                .Include(e => e.Cargo).ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(e => e.CodCedula == employeeId);

            if (employee == null)
                return new EmployeeLogStatusDto { Status = "error", ErrorMessage = "Empleado no encontrado" };

            string? unitType = employee.Cargo?.Unidad?.TipoUnidad;

            var openToday = await _context.SegRegistroEmpleados
                .FirstOrDefaultAsync(l => l.CodCedula == employeeId &&
                                          l.FechaEntrada == currentDate &&
                                          l.IndicadorEntrada && !l.IndicadorSalida);

            var completeToday = await _context.SegRegistroEmpleados
                .FirstOrDefaultAsync(l => l.CodCedula == employeeId &&
                                          l.FechaEntrada == currentDate &&
                                          l.IndicadorEntrada && l.IndicadorSalida);

            SegRegistroEmpleado? openYesterday = null;
            if (unitType == "O")
            {
                openYesterday = await _context.SegRegistroEmpleados
                    .FirstOrDefaultAsync(l => l.CodCedula == employeeId &&
                                              l.FechaEntrada == currentDate.AddDays(-1) &&
                                              l.IndicadorEntrada && !l.IndicadorSalida);
            }

            if (openToday != null)
                return EmployeeLogMappers.ToStateDto("openEntry", openToday, currentDate, currentTime, unitType);

            if (completeToday != null)
                return EmployeeLogMappers.ToStateDto("completeEntry", completeToday, currentDate, currentTime, unitType);

            if (openYesterday != null)
                return EmployeeLogMappers.ToStateDto("openEntryYesterday", openYesterday, currentDate, currentTime, unitType);

            return new EmployeeLogStatusDto
            {
                Status = "noEntry",
                CurrentDate = currentDate.ToString("yyyy-MM-dd"),
                CurrentTime = currentTime.ToString("HH:mm"),
                UnitType = unitType
            };
        }

        public async Task<ServiceResult> RecordEmployeeEntryExitAsync(
            EmployeeLogEntryViewModel vm, string currentUserId, string? confirmedValidation)
        {
            var basicCheck = EmployeeLogValidators.ValidateEntryInputs(vm);
            if (basicCheck is not null) return basicCheck;

            // Parse
            var parsedEntryDate = DateOnly.ParseExact(vm.EntryDateStr!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var parsedEntryTime = TimeOnly.ParseExact(vm.EntryTimeStr!, "HH:mm", CultureInfo.InvariantCulture);
            var now = DateTime.Now;

            // Reglas temporales
            var temporalError = EmployeeLogValidators.ValidateEntryTemporalRules(parsedEntryDate, parsedEntryTime, now);
            if (temporalError is not null) return temporalError;

            // Maestro empleado
            var employee = await _context.AdmEmpleados
                .Include(e => e.Cargo).ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(e => e.CodCedula == vm.EmployeeId);
            if (employee == null)
                return ServiceResult.FailureResult("Empleado no encontrado.");

            var entryDateTimeClient = parsedEntryDate.ToDateTime(parsedEntryTime);

            // Si viene combinado, validar salida
            DateOnly? parsedExitDate = null; TimeOnly? parsedExitTime = null;
            if (vm.IsExitRecorded)
            {
                if (string.IsNullOrWhiteSpace(vm.ExitDateStr) || string.IsNullOrWhiteSpace(vm.ExitTimeStr))
                    return ServiceResult.FailureResult("Debe proporcionar fecha y hora de salida para registro combinado.");

                parsedExitDate = DateOnly.ParseExact(vm.ExitDateStr!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                parsedExitTime = TimeOnly.ParseExact(vm.ExitTimeStr!, "HH:mm", CultureInfo.InvariantCulture);

                var exitCheck = EmployeeLogValidators.ValidateExitAgainstEntry(
                    entryDateTimeClient, parsedExitDate.Value.ToDateTime(parsedExitTime.Value), now, confirmedValidation ?? vm.ConfirmedValidation);
                if (exitCheck is not null) return exitCheck;
            }

            // Reglas de unicidad / conflictos
            var conflictCheck = await EmployeeLogValidators.ValidateNewEntryConflictsAsync(
                _context, vm.EmployeeId!.Value, entryDateTimeClient, DateOnly.FromDateTime(now.Date));
            if (conflictCheck is not null) return conflictCheck;

            // Branch: aplicar alcance y resolver branch a usar
            var codSucursal = ResolveBranchToUse(vm.BranchId);

            // INSERT
            var newLog = EmployeeLogMappers.ToNewEntity(vm, parsedEntryDate, parsedEntryTime, parsedExitDate, parsedExitTime, codSucursal, currentUserId);
            try
            {
                await _context.SegRegistroEmpleados.AddAsync(newLog);
                await _context.SaveChangesAsync();

                if (newLog.IndicadorEntrada)
                    await _dailyRoute.UpdateAsync(newLog.CodCedula, newLog.FechaEntrada, newLog.HoraEntrada, true, currentUserId);

                if (newLog.IndicadorSalida && newLog.FechaSalida.HasValue && newLog.HoraSalida.HasValue)
                    await _dailyRoute.UpdateAsync(newLog.CodCedula, newLog.FechaSalida.Value, newLog.HoraSalida.Value, false, currentUserId);

                Log.Information("EMPLOG: NEW {Id} emp={Ced} in={In} out={Out}", newLog.Id, newLog.CodCedula, newLog.IndicadorEntrada, newLog.IndicadorSalida);
                return ServiceResult.SuccessResult("Registro agregado exitosamente.", newLog.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "EMPLOG: error insert emp={Ced}", vm.EmployeeId);
                return ServiceResult.FailureResult("Error al guardar el registro: " + ex.Message);
            }
        }

        public async Task<ServiceResult> UpdateEmployeeLogAsync(
            EmployeeLogEditViewModel vm, string currentUserId, string? confirmedValidation)
        {
            if (vm.EmployeeId <= 0)
                return ServiceResult.FailureResult("Empleado inválido.");

            var now = DateTime.Now;
            var entryDateTime = vm.EntryDate.ToDateTime(vm.EntryTime);

            var entryTemporalCheck = EmployeeLogValidators.ValidateEditEntryTemporalRules(entryDateTime, now);
            if (entryTemporalCheck is not null) return entryTemporalCheck;

            // salida opcional
            DateTime? exitDateTime = (vm.ExitDate.HasValue && vm.ExitTime.HasValue)
                ? vm.ExitDate.Value.ToDateTime(vm.ExitTime.Value)
                : null;

            if (exitDateTime.HasValue)
            {
                var exitCheck = EmployeeLogValidators.ValidateExitAgainstEntry(
                    entryDateTime, exitDateTime.Value, now, confirmedValidation ?? vm.ConfirmedValidation);
                if (exitCheck is not null) return exitCheck;
            }

            var existing = await _context.SegRegistroEmpleados
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(r => r.Id == vm.Id);

            if (existing == null)
                return ServiceResult.FailureResult("Registro no encontrado para actualización.");

            // Alcance por sucursal
            if (!IsEntityInScope(existing.CodSucursal))
                return ServiceResult.FailureResult("No tiene permisos para modificar registros de esta sucursal.");

            if (!EmployeeLogValidators.HasChanges(existing, vm.EntryDate, vm.EntryTime, vm.ExitDate, vm.ExitTime))
                return ServiceResult.FailureResult("No se realizaron cambios en el registro.");

            // Reglas por tipo de unidad y conflictos contra otros
            var scopeCheck = await EmployeeLogValidators.ValidateEditConflictsAsync(_context, vm, existing);
            if (scopeCheck is not null) return scopeCheck;

            // Actualizar
            existing.FechaEntrada = vm.EntryDate;
            existing.HoraEntrada = vm.EntryTime;
            existing.FechaSalida = vm.ExitDate;
            existing.HoraSalida = vm.ExitTime;
            existing.IndicadorEntrada = true;
            existing.IndicadorSalida = vm.ExitDate.HasValue && vm.ExitTime.HasValue;
            existing.RegistroUsuarioId = currentUserId;

            // Refrescar nombres desde maestro (opcional)
            var emp = await _context.AdmEmpleados.Include(e => e.Cargo).ThenInclude(c => c.Unidad)
                                                 .Include(e => e.Sucursal)
                                                 .FirstOrDefaultAsync(e => e.CodCedula == vm.EmployeeId);
            if (emp != null) EmployeeLogMappers.RefreshNames(existing, emp);

            try
            {
                _context.SegRegistroEmpleados.Update(existing);
                await _context.SaveChangesAsync();

                if (existing.IndicadorSalida && existing.FechaSalida.HasValue && existing.HoraSalida.HasValue)
                    await _dailyRoute.UpdateAsync(existing.CodCedula, existing.FechaSalida.Value, existing.HoraSalida.Value, false, currentUserId);

                return ServiceResult.SuccessResult("Registro actualizado exitosamente.", vm.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "EMPLOG: error update id={Id}", vm.Id);
                return ServiceResult.FailureResult("Error al actualizar el registro.");
            }
        }

        public async Task<ServiceResult> RecordManualEmployeeExitAsync(
            EmployeeLogManualExitViewModel vm, string currentUserId, string? confirmedValidation)
        {
            if (!vm.ExitDate.HasValue || !vm.ExitTime.HasValue)
                return ServiceResult.FailureResult("La fecha y hora de salida son obligatorias.");

            var now = DateTime.Now;

            var existing = await _context.SegRegistroEmpleados
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(r => r.Id == vm.Id);

            if (existing == null)
                return ServiceResult.FailureResult("Registro no encontrado.");

            if (existing.IndicadorSalida && existing.FechaSalida.HasValue && existing.HoraSalida.HasValue)
                return ServiceResult.FailureResult("Este registro ya tiene salida.");

            if (!IsEntityInScope(existing.CodSucursal))
                return ServiceResult.FailureResult("No tiene permisos para modificar registros de esta sucursal.");

            var entryDT = existing.FechaEntrada.ToDateTime(existing.HoraEntrada);
            var exitDT = vm.ExitDate.Value.ToDateTime(vm.ExitTime.Value);

            if (existing.Empleado?.Cargo?.Unidad?.TipoUnidad == "A" && vm.ExitDate.Value != existing.FechaEntrada)
                return ServiceResult.FailureResult("Para tipo 'A', la fecha de salida debe ser igual a la de entrada.");

            var exitCheck = EmployeeLogValidators.ValidateExitAgainstEntry(entryDT, exitDT, now, confirmedValidation ?? vm.ConfirmedValidation);
            if (exitCheck is not null) return exitCheck;

            existing.FechaSalida = vm.ExitDate;
            existing.HoraSalida = vm.ExitTime;
            existing.IndicadorSalida = true;
            existing.RegistroUsuarioId = currentUserId;

            try
            {
                _context.SegRegistroEmpleados.Update(existing);
                await _context.SaveChangesAsync();

                await _dailyRoute.UpdateAsync(existing.CodCedula, vm.ExitDate.Value, vm.ExitTime.Value, false, currentUserId);
                return ServiceResult.SuccessResult("Salida registrada exitosamente.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "EMPLOG: error manual exit id={Id}", vm.Id);
                return ServiceResult.FailureResult("Error al registrar la salida manual: " + ex.Message);
            }
        }
        // -------------------- CRUD (DTO wrappers) --------------------

        public Task<ServiceResult> RecordEmployeeEntryExitAsync(
            EmployeeLogDto dto, string currentUserId, string? confirmedValidation)
            => RecordEmployeeEntryExitAsync(EmployeeLogMappers.ToEntryVm(dto), currentUserId, confirmedValidation);

        public Task<ServiceResult> UpdateEmployeeLogAsync(
            int logId, EmployeeLogDto dto, string currentUserId, string? confirmedValidation)
        {
            var vm = EmployeeLogMappers.ToEditVmFromDto(logId, dto);
            return UpdateEmployeeLogAsync(vm, currentUserId, confirmedValidation);
        }

        public Task<ServiceResult> RecordManualEmployeeExitAsync(
            int logId, DateOnly exitDate, TimeOnly exitTime, string currentUserId, string? confirmedValidation)
            => RecordManualEmployeeExitAsync(new EmployeeLogManualExitViewModel { Id = logId, ExitDate = exitDate, ExitTime = exitTime }, currentUserId, confirmedValidation);

        // -------------------- LISTADOS (sin SP) --------------------
        public async Task<(IEnumerable<EmployeeLogListDto> Items, int Total)> GetFilteredEmployeeLogsAsync(
            string currentUserId, int? cargoId, string? unitId, int? branchId,
            DateOnly? startDate, DateOnly? endDate, int? logStatus,
            string? search, int page, int pageSize, bool isAdmin)
        {
            var baseQuery = _context.SegRegistroEmpleados
                .AsNoTracking()
                .ApplyBranchScope(_branch);

            if (cargoId.HasValue)
                baseQuery = baseQuery.Where(r => r.CodCargo == cargoId.Value);

            if (!string.IsNullOrWhiteSpace(unitId))
                baseQuery = baseQuery.Where(r => r.CodUnidad == unitId);

            if (branchId.HasValue)
                baseQuery = baseQuery.Where(r => r.CodSucursal == branchId.Value);

            if (startDate.HasValue)
                baseQuery = baseQuery.Where(r => r.FechaEntrada >= startDate.Value);

            if (endDate.HasValue)
                baseQuery = baseQuery.Where(r => r.FechaEntrada <= endDate.Value);

            if (logStatus.HasValue)
            {
                if (logStatus.Value == 0)
                    baseQuery = baseQuery.Where(r => r.IndicadorEntrada && !r.IndicadorSalida);
                else if (logStatus.Value == 1)
                    baseQuery = baseQuery.Where(r => r.IndicadorEntrada && r.IndicadorSalida);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                baseQuery = baseQuery.Where(r =>
                    EF.Functions.Like(r.PrimerNombreEmpleado ?? "", $"%{search}%") ||
                    EF.Functions.Like(r.PrimerApellidoEmpleado ?? "", $"%{search}%") ||
                    EF.Functions.Like(r.SegundoApellidoEmpleado ?? "", $"%{search}%") ||
                    EF.Functions.Like(r.NombreCargoEmpleado ?? "", $"%{search}%") ||
                    r.CodCedula.ToString().Contains(search)
                );
            }

            var total = await baseQuery.CountAsync();

            var rows = await baseQuery
                .OrderByDescending(r => r.FechaEntrada)
                .ThenByDescending(r => r.HoraEntrada)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new EmployeeLogListDto
                {
                    Id = r.Id,
                    CodCedula = r.CodCedula,

                    PrimerNombreEmpleado = r.PrimerNombreEmpleado,
                    SegundoNombreEmpleado = r.SegundoNombreEmpleado,
                    PrimerApellidoEmpleado = r.PrimerApellidoEmpleado,
                    SegundoApellidoEmpleado = r.SegundoApellidoEmpleado,

                    NombreCompletoEmpleado =
                        (r.PrimerNombreEmpleado ?? "") + " " +
                        (r.SegundoNombreEmpleado ?? "") + " " +
                        (r.PrimerApellidoEmpleado ?? "") + " " +
                        (r.SegundoApellidoEmpleado ?? ""),

                    NombreCargoEmpleado = r.NombreCargoEmpleado,
                    NombreUnidadEmpleado = r.NombreUnidadEmpleado,
                    NombreSucursalEmpleado = r.NombreSucursalEmpleado,

                    FechaEntrada = r.FechaEntrada,
                    HoraEntrada = r.HoraEntrada,
                    FechaSalida = r.FechaSalida,
                    HoraSalida = r.HoraSalida,
                    IndicadorEntrada = r.IndicadorEntrada,
                    IndicadorSalida = r.IndicadorSalida,

                    UsuarioRegistroNombre = r.UsuarioRegistro != null
                        ? r.UsuarioRegistro.UserName
                        : null
                })
                .ToListAsync();

            return (rows, total);
        }

        public async Task<List<EmployeeLogSearchDto>> GetEmployeeInfoAsync(
            string userId, List<int> permittedBranchIds, string? searchInput, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(searchInput))
                return new List<EmployeeLogSearchDto>();

            var q = _context.AdmEmpleados
                .AsNoTracking()
                .Where(e => e.EmpleadoEstado == EstadoEmpleado.Activo)
                .ScopedEmployees(_branch);

            var cleanSearch = searchInput.Trim();
            var words = cleanSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (words.Length > 1)
            {
                foreach (var word in words)
                {
                    q = q.Where(e =>
                        EF.Functions.Like(e.NombreCompleto ?? "", $"%{word}%") ||
                        EF.Functions.Like(e.PrimerNombre ?? "", $"%{word}%") ||
                        EF.Functions.Like(e.SegundoNombre ?? "", $"%{word}%") ||
                        EF.Functions.Like(e.PrimerApellido ?? "", $"%{word}%") ||
                        EF.Functions.Like(e.SegundoApellido ?? "", $"%{word}%") ||
                        EF.Functions.Like(e.NumeroCarnet ?? "", $"%{word}%") ||
                        EF.Functions.Like(e.CodCedula.ToString(), $"%{word}%")
                    );
                }
            }
            else
            {
                q = q.Where(e =>
                    EF.Functions.Like(e.NombreCompleto ?? "", $"%{cleanSearch}%") ||
                    EF.Functions.Like(e.PrimerNombre ?? "", $"%{cleanSearch}%") ||
                    EF.Functions.Like(e.SegundoNombre ?? "", $"%{cleanSearch}%") ||
                    EF.Functions.Like(e.PrimerApellido ?? "", $"%{cleanSearch}%") ||
                    EF.Functions.Like(e.SegundoApellido ?? "", $"%{cleanSearch}%") ||
                    EF.Functions.Like(e.NumeroCarnet ?? "", $"%{cleanSearch}%") ||
                    EF.Functions.Like(e.CodCedula.ToString(), $"%{cleanSearch}%")
                );
            }

            if (permittedBranchIds?.Count > 0)
                q = q.Where(e => e.CodSucursal.HasValue
                    && permittedBranchIds.Contains(e.CodSucursal.Value));

            var employees = await q
                .OrderBy(e => EF.Functions.Like(e.NombreCompleto ?? "", $"{cleanSearch}") ? 0 : 1)
                    .ThenBy(e => EF.Functions.Like(e.NombreCompleto ?? "", $"{cleanSearch}%") ? 0 : 1)
                .Take(20)
                .Select(e => new EmployeeLogSearchDto
                {
                    CodCedula = e.CodCedula,
                    PrimerNombre = e.PrimerNombre,
                    SegundoNombre = e.SegundoNombre,
                    PrimerApellido = e.PrimerApellido,
                    SegundoApellido = e.SegundoApellido,
                    NombreCompleto = e.NombreCompleto,
                    CodCargo = e.CodCargo,
                    CargoNombre = e.Cargo != null ? e.Cargo.NombreCargo : null,
                    CodUnidad = e.Cargo != null && e.Cargo.Unidad != null
                        ? e.Cargo.Unidad.CodUnidad
                        : null,
                    UnidadNombre = e.Cargo != null && e.Cargo.Unidad != null
                        ? e.Cargo.Unidad.NombreUnidad
                        : null,
                    TipoUnidad = e.Cargo != null && e.Cargo.Unidad != null
                        ? e.Cargo.Unidad.TipoUnidad
                        : null,
                    CodSucursal = e.CodSucursal,
                    SucursalNombre = e.Sucursal != null ? e.Sucursal.NombreSucursal : null,
                    FotoUrl = e.FotoUrl
                })
                .ToListAsync();

            return employees;
        }

        public Task<SegRegistroEmpleado?> GetLogByIdAsync(int id)
            => _context.SegRegistroEmpleados
                .Include(e => e.Empleado).ThenInclude(c => c.Cargo).ThenInclude(u => u.Unidad)
                .Include(e => e.Sucursal)
                .FirstOrDefaultAsync(e => e.Id == id);

        // -------------------- Helpers Branch --------------------

        private int ResolveBranchToUse(int? vmBranchId)
        {
            if (_branch.CurrentBranchId.HasValue)
                return _branch.CurrentBranchId.Value;

            if (_branch.AllBranches && vmBranchId.HasValue && _branch.PermittedBranchIds.Contains(vmBranchId.Value))
                return vmBranchId.Value;

            throw new InvalidOperationException("No tiene permisos sobre la sucursal seleccionada.");
        }

        private bool IsEntityInScope(int entityBranchId)
        {
            if (_branch.CurrentBranchId.HasValue) return _branch.CurrentBranchId.Value == entityBranchId;
            if (_branch.AllBranches) return _branch.PermittedBranchIds.Contains(entityBranchId);
            return false;
        }
    }
}