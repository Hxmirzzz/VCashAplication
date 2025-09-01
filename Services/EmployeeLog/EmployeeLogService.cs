using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data;
using System.Data.Common;
using System.Globalization;
using VCashApp.Data;
using VCashApp.Models;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.EmployeeLog;
using VCashApp.Services.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;


namespace VCashApp.Services.EmployeeLog
{
    /// <summary>
    /// Servicio de negocio para Registro de Empleados (Entrada/Salida),
    /// estructurado al estilo de CefContainerService:
    ///  - Preparación de páginas (ViewModels)
    ///  - Guardado/Actualización (vía DTO y vía VM)
    ///  - Consultas/Reportes auxiliares
    ///  - Mapeadores internos y helpers
    /// </summary>
    public class EmployeeLogService : IEmployeeLogService
    {
        private readonly AppDbContext _context;
        private readonly IRutaDiariaService _rutaDiariaService;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeLogService(AppDbContext context, IRutaDiariaService rutaDiariaService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _rutaDiariaService = rutaDiariaService;
            _userManager = userManager;
        }

        // ---------------------------------------------------------------------
        //  PREPARACIÓN DE PÁGINAS / VIEWMODELS
        // ---------------------------------------------------------------------

        public Task<EmployeeLogEntryViewModel> GetEntryViewModelAsync(
            string userName, string? unidadName, string? branchName, string? fullName,
            bool canCreate, bool canEdit)
        {
            // Equivalente a "PrepareProcessContainersPageAsync" pero para entrada:
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

            return new EmployeeLogEditViewModel
            {
                Id = s.Id,
                EmployeeId = s.CodCedula,
                EmployeeFullName = s.Empleado?.NombreCompleto ??
                                   $"{s.PrimerNombreEmpleado} {s.SegundoNombreEmpleado} {s.PrimerApellidoEmpleado} {s.SegundoApellidoEmpleado}".Replace("  ", " ").Trim(),
                PhotoUrl = s.Empleado?.FotoUrl,
                CargoId = s.Empleado?.CodCargo,
                CargoName = s.NombreCargoEmpleado ?? s.Empleado?.Cargo?.NombreCargo,
                UnitId = s.Empleado?.Cargo?.Unidad?.CodUnidad,
                UnitName = s.NombreUnidadEmpleado ?? s.Empleado?.Cargo?.Unidad?.NombreUnidad,
                UnitType = s.Empleado?.Cargo?.Unidad?.TipoUnidad,
                BranchId = s.CodSucursal,
                BranchName = s.NombreSucursalEmpleado ?? s.Sucursal?.NombreSucursal,
                EntryDate = s.FechaEntrada,
                EntryTime = s.HoraEntrada,
                ExitDate = s.FechaSalida,
                ExitTime = s.HoraSalida,
                IsEntryRecorded = true,
                IsExitRecorded = s.FechaSalida.HasValue && s.HoraSalida.HasValue,
                CanEditLog = canEditLog
            };
        }

        public async Task<EmployeeLogDetailsViewModel?> GetDetailsViewModelAsync(int id)
        {
            var s = await _context.SegRegistroEmpleados.AsNoTracking()
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(r => r.Sucursal)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (s == null) return null;

            return new EmployeeLogDetailsViewModel
            {
                Id = s.Id,
                EmployeeId = s.CodCedula,
                EmployeeFullName = s.Empleado?.NombreCompleto ??
                                   $"{s.PrimerNombreEmpleado} {s.PrimerApellidoEmpleado}".Trim(),
                PhotoUrl = s.Empleado?.FotoUrl,
                CargoName = s.NombreCargoEmpleado ?? s.Empleado?.Cargo?.NombreCargo,
                UnitName = s.NombreUnidadEmpleado ?? s.Empleado?.Cargo?.Unidad?.NombreUnidad,
                BranchName = s.NombreSucursalEmpleado ?? s.Sucursal?.NombreSucursal,
                EntryDate = s.FechaEntrada,
                EntryTime = s.HoraEntrada,
                ExitDate = s.FechaSalida,
                ExitTime = s.HoraSalida
            };
        }

        public async Task<EmployeeLogManualExitViewModel?> GetManualExitViewModelAsync(int id, bool canCreateLog, bool canEditLog)
        {
            var s = await _context.SegRegistroEmpleados.AsNoTracking()
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(r => r.Sucursal)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (s == null) return null;

            // Igual que hacías en el controlador: por defecto propone “ahora”
            return new EmployeeLogManualExitViewModel
            {
                Id = s.Id,
                EmployeeId = s.CodCedula,
                EmployeeFullName = s.Empleado?.NombreCompleto ??
                                   $"{s.PrimerNombreEmpleado} {s.PrimerApellidoEmpleado}".Trim(),
                PhotoUrl = s.Empleado?.FotoUrl,
                CargoId = s.Empleado?.CodCargo,
                CargoName = s.NombreCargoEmpleado ?? s.Empleado?.Cargo?.NombreCargo,
                UnitId = s.Empleado?.Cargo?.Unidad?.CodUnidad,
                UnitName = s.NombreUnidadEmpleado ?? s.Empleado?.Cargo?.Unidad?.NombreUnidad,
                UnitType = s.Empleado?.Cargo?.Unidad?.TipoUnidad,
                BranchId = s.CodSucursal,
                BranchName = s.Sucursal?.NombreSucursal ?? s.NombreSucursalEmpleado,
                EntryDate = s.FechaEntrada,
                EntryTime = s.HoraEntrada,
                ExitDate = DateOnly.FromDateTime(DateTime.Now.Date),   // propuesta
                ExitTime = TimeOnly.FromDateTime(DateTime.Now),        // propuesta
                CanCreateLog = canCreateLog,
                CanEditLog = canEditLog
            };
        }

        // =========================================================
        // B) GUARDAR / ACTUALIZAR  (flujos por ViewModel)
        // =========================================================

        public async Task<ServiceResult> RecordEmployeeEntryExitAsync(EmployeeLogEntryViewModel vm, string currentUserId, string? confirmedValidation)
        {
            string entryDateString = vm.EntryDateStr ?? vm.EntryDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            string entryTimeString = vm.EntryTimeStr ?? vm.EntryTime?.ToString("HH:mm") ?? string.Empty;

            if (vm.EmployeeId is null || vm.EmployeeId <= 0)
                return ServiceResult.FailureResult("Debe indicar la cédula del empleado.");
            if (string.IsNullOrWhiteSpace(entryDateString))
                return ServiceResult.FailureResult("La fecha de entrada no es válida o está vacía.");
            if (string.IsNullOrWhiteSpace(entryTimeString))
                return ServiceResult.FailureResult("La hora de entrada no es válida o está vacía.");

            DateOnly parsedEntryDate;
            TimeOnly parsedEntryTime;
            try
            {
                parsedEntryDate = DateOnly.ParseExact(entryDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                parsedEntryTime = TimeOnly.ParseExact(entryTimeString, "HH:mm", CultureInfo.InvariantCulture);
            }
            catch
            {
                return ServiceResult.FailureResult("Error de formato en la fecha u hora de entrada. Formato esperado: yyyy-MM-dd / HH:mm.");
            }

            var now = DateTime.Now;
            var currentDateNow = DateOnly.FromDateTime(now.Date);

            Log.Information("SERVICE: RecordEmployeeEntryExitAsync(VM) - Start. EmployeeId={EmployeeId}, Entry={Entry}, Exit={Exit}, Confirmed='{Confirmed}'",
                vm.EmployeeId, vm.IsEntryRecorded, vm.IsExitRecorded, confirmedValidation ?? vm.ConfirmedValidation ?? "NONE");

            var employee = await _context.AdmEmpleados
                .Include(e => e.Cargo).ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(e => e.CodCedula == vm.EmployeeId);

            if (employee == null)
                return ServiceResult.FailureResult("Empleado no encontrado.");

            string? employeeUnitType = employee.Cargo?.Unidad?.TipoUnidad;

            var entryDateTimeClient = parsedEntryDate.ToDateTime(parsedEntryTime);
            if (entryDateTimeClient > now.AddMinutes(5))
                return ServiceResult.FailureResult("No se puede registrar una entrada con fecha/hora futura.");
            if ((now - entryDateTimeClient).TotalDays > 15)
                return ServiceResult.FailureResult("No se puede registrar una entrada con más de 15 días de antigüedad.");

            // Registro combinado
            if (vm.IsEntryRecorded && vm.IsExitRecorded)
            {
                var exitDateString = vm.ExitDateStr ?? vm.ExitDate?.ToString("yyyy-MM-dd");
                var exitTimeString = vm.ExitTimeStr ?? vm.ExitTime?.ToString("HH:mm");

                if (string.IsNullOrWhiteSpace(exitDateString) || string.IsNullOrWhiteSpace(exitTimeString))
                    return ServiceResult.FailureResult("Debe proporcionar fecha y hora de salida para registro combinado.");

                DateOnly parsedExitDate;
                TimeOnly parsedExitTime;
                try
                {
                    parsedExitDate = DateOnly.ParseExact(exitDateString!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    parsedExitTime = TimeOnly.ParseExact(exitTimeString!, "HH:mm", CultureInfo.InvariantCulture);
                }
                catch
                {
                    return ServiceResult.FailureResult("Error de formato en fecha/hora de salida. Formato esperado: yyyy-MM-dd / HH:mm.");
                }

                var exitDateTimeClient = parsedExitDate.ToDateTime(parsedExitTime);
                if (exitDateTimeClient <= entryDateTimeClient)
                    return ServiceResult.FailureResult("La salida debe ser posterior a la entrada.");
                if (exitDateTimeClient > now.AddMinutes(5))
                    return ServiceResult.FailureResult("No se puede registrar una salida futura en un combinado.");

                var worked = exitDateTimeClient - entryDateTimeClient;
                var hours = worked.TotalHours;

                if (hours <= 0) return ServiceResult.FailureResult("Horas trabajadas deben ser positivas.");
                if (hours > 23) return ServiceResult.FailureResult($"No puede exceder 23 horas continuas. ({hours:F2})");
                if (worked.TotalDays > 3) return ServiceResult.FailureResult("Un turno no puede exceder 3 días consecutivos.");

                if ((hours < 4 || hours > 20) &&
                    (confirmedValidation ?? vm.ConfirmedValidation) is not ("minHours" or "maxHours"))
                {
                    var validationType = hours < 4 ? "minHours" : "maxHours";
                    var msg = hours < 4
                        ? $"El empleado debe trabajar mínimo 4 horas. Horas registradas: {hours:F2}"
                        : $"El empleado no puede trabajar más de 20 horas continuas. Horas registradas: {hours:F2}";
                    return ServiceResult.ConfirmationRequired(msg, validationType, hours);
                }
            }

            // estado actual
            var existingOpenLogAnyDate = await _context.SegRegistroEmpleados
                .Where(l => l.CodCedula == vm.EmployeeId && l.IndicadorEntrada && !l.IndicadorSalida)
                .OrderByDescending(l => l.FechaEntrada).ThenByDescending(l => l.HoraEntrada)
                .FirstOrDefaultAsync();

            var completeLogToday = await _context.SegRegistroEmpleados
                .Where(l => l.CodCedula == vm.EmployeeId &&
                            l.FechaEntrada == currentDateNow &&
                            l.IndicadorEntrada && l.IndicadorSalida)
                .FirstOrDefaultAsync();

            if (existingOpenLogAnyDate != null)
                return ServiceResult.FailureResult($"Este empleado ya tiene una entrada abierta (registrada el {existingOpenLogAnyDate.FechaEntrada:dd/MM/yyyy}).");

            if (completeLogToday != null)
                return ServiceResult.FailureResult("Ya existe un registro completo hoy. No se permite más de una entrada por día.");

            // espacio vs última salida completa
            var ultimoRegistroCompleto = await _context.SegRegistroEmpleados
                .Where(r => r.CodCedula == vm.EmployeeId && r.FechaSalida.HasValue && r.HoraSalida.HasValue)
                .OrderByDescending(r => r.FechaSalida).ThenByDescending(r => r.HoraSalida)
                .FirstOrDefaultAsync();

            if (ultimoRegistroCompleto != null)
            {
                var ultimaSalida = ultimoRegistroCompleto.FechaSalida!.Value.ToDateTime(ultimoRegistroCompleto.HoraSalida!.Value);
                if ((entryDateTimeClient - ultimaSalida).TotalHours < 1)
                    return ServiceResult.FailureResult("Debe haber al menos 1 hora entre la salida anterior y la nueva entrada.");
            }

            // INSERT
            var newLog = new SegRegistroEmpleado
            {
                CodCedula = vm.EmployeeId.Value,
                PrimerNombreEmpleado = vm.FirstName,
                SegundoNombreEmpleado = vm.MiddleName,
                PrimerApellidoEmpleado = vm.LastName,
                SegundoApellidoEmpleado = vm.SecondLastName,
                CodCargo = vm.CargoId,
                NombreCargoEmpleado = vm.CargoName,
                CodUnidad = vm.UnitId,
                NombreUnidadEmpleado = vm.UnitName,
                CodSucursal = vm.BranchId ?? 0,
                NombreSucursalEmpleado = vm.BranchName,
                FechaEntrada = parsedEntryDate,
                HoraEntrada = parsedEntryTime,
                FechaSalida = vm.ExitDateStr != null ? DateOnly.ParseExact(vm.ExitDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture) : vm.ExitDate,
                HoraSalida = vm.ExitTimeStr != null ? TimeOnly.ParseExact(vm.ExitTimeStr, "HH:mm", CultureInfo.InvariantCulture) : vm.ExitTime,
                IndicadorEntrada = vm.IsEntryRecorded,
                IndicadorSalida = vm.IsExitRecorded,
                RegistroUsuarioId = currentUserId
            };

            try
            {
                await _context.SegRegistroEmpleados.AddAsync(newLog);
                await _context.SaveChangesAsync();

                if (newLog.IndicadorEntrada)
                    await UpdateDailyRouteJTLogAsync(newLog.CodCedula, newLog.FechaEntrada, newLog.HoraEntrada, true, currentUserId);

                if (newLog.IndicadorSalida && newLog.FechaSalida.HasValue && newLog.HoraSalida.HasValue)
                    await UpdateDailyRouteJTLogAsync(newLog.CodCedula, newLog.FechaSalida.Value, newLog.HoraSalida.Value, false, currentUserId);

                Log.Information("SERVICE: {EmployeeId} | NEW LOG ID:{Id} inserted. In={In} Out={Out}",
                    newLog.CodCedula, newLog.Id, newLog.IndicadorEntrada, newLog.IndicadorSalida);

                return ServiceResult.SuccessResult("Registro agregado exitosamente.", newLog.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DB error saving new log for {EmployeeId}", vm.EmployeeId);
                return ServiceResult.FailureResult("Error al guardar el registro: " + ex.Message);
            }
        }

        public async Task<ServiceResult> UpdateEmployeeLogAsync(EmployeeLogEditViewModel vm, string currentUserId, string? confirmedValidation)
        {
            if (vm.EmployeeId <= 0)
                return ServiceResult.FailureResult("Empleado inválido.");

            // Entrada (DateOnly/TimeOnly vienen en VM)
            var entryDate = vm.EntryDate;
            var entryTime = vm.EntryTime;

            var now = DateTime.Now;
            var entryDateTime = entryDate.ToDateTime(entryTime);

            if (entryDateTime > now.AddMinutes(5))
                return ServiceResult.FailureResult("La fecha/hora de entrada no pueden ser futuras.");
            if ((now - entryDateTime).TotalDays > 15)
                return ServiceResult.FailureResult("No se pueden actualizar entradas con más de 15 días de antigüedad.");

            // Salida (opcional, pero si viene, viene completa)
            DateOnly? exitDate = vm.ExitDate;
            TimeOnly? exitTime = vm.ExitTime;
            DateTime? exitDateTime = (exitDate.HasValue && exitTime.HasValue) ? exitDate.Value.ToDateTime(exitTime.Value) : null;

            if ((exitDate.HasValue && !exitTime.HasValue) || (!exitDate.HasValue && exitTime.HasValue))
                return ServiceResult.FailureResult("Si especifica fecha de salida, debe especificar también la hora (y viceversa).");

            if (exitDateTime.HasValue)
            {
                if (exitDateTime.Value <= entryDateTime)
                    return ServiceResult.FailureResult("La salida debe ser posterior a la entrada.");
                if (exitDateTime.Value > now.AddMinutes(5))
                    return ServiceResult.FailureResult("La fecha/hora de salida no puede ser futura.");

                var worked = exitDateTime.Value - entryDateTime;
                var hours = worked.TotalHours;

                if (hours <= 0) return ServiceResult.FailureResult("Horas trabajadas deben ser positivas.");
                if (hours > 23) return ServiceResult.FailureResult($"No puede exceder 23 horas continuas. ({hours:F2})");
                if (worked.TotalDays > 3) return ServiceResult.FailureResult("Un turno no puede exceder 3 días consecutivos.");

                if ((hours < 4 || hours > 20) &&
                    (confirmedValidation ?? vm.ConfirmedValidation) is not ("minHours" or "maxHours"))
                {
                    var validationType = hours < 4 ? "minHours" : "maxHours";
                    var msg = hours < 4
                        ? $"El empleado debe trabajar mínimo 4 horas. Horas registradas: {hours:F2}"
                        : $"El empleado no puede trabajar más de 20 horas continuas. Horas registradas: {hours:F2}";
                    return ServiceResult.ConfirmationRequired(msg, validationType, hours);
                }
            }

            var existing = await _context.SegRegistroEmpleados
                .Include(r => r.Empleado).ThenInclude(e => e.Cargo).ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(r => r.Id == vm.Id);

            if (existing == null)
                return ServiceResult.FailureResult("Registro no encontrado para actualización.");

            var employeeUnitType = existing.Empleado?.Cargo?.Unidad?.TipoUnidad;

            if (!HasChanges(existing, entryDate, entryTime, exitDate, exitTime))
                return ServiceResult.FailureResult("No se realizaron cambios en el registro.");

            // Reglas de día por tipo de unidad
            var logsSameDayExceptCurrent = await _context.SegRegistroEmpleados
                .Where(r => r.CodCedula == vm.EmployeeId && r.Id != vm.Id && r.FechaEntrada == entryDate)
                .ToListAsync();

            if (employeeUnitType == "A" && logsSameDayExceptCurrent.Any())
                return ServiceResult.FailureResult($"Un empleado tipo 'A' no puede tener múltiples entradas el {entryDate:dd/MM/yyyy}.");

            if (employeeUnitType == "O" && logsSameDayExceptCurrent.Count >= 2)
                return ServiceResult.FailureResult($"Un empleado tipo 'O' no puede tener más de 2 entradas el {entryDate:dd/MM/yyyy}.");

            // Otra entrada abierta (excluyendo la actual) en fecha actual
            var openLogExcluding = await _context.SegRegistroEmpleados
                .Where(l => l.CodCedula == vm.EmployeeId && l.Id != vm.Id && l.IndicadorEntrada && !l.IndicadorSalida)
                .FirstOrDefaultAsync();

            var currentDate = DateOnly.FromDateTime(now.Date);
            if (openLogExcluding != null && entryDate == currentDate)
                return ServiceResult.FailureResult($"Este empleado ya tiene una entrada abierta (ID: {openLogExcluding.Id}).");

            // Conflictos temporales con otros registros del empleado
            var others = await _context.SegRegistroEmpleados
                .Where(r => r.CodCedula == vm.EmployeeId && r.Id != vm.Id)
                .Select(r => new { r.Id, r.FechaEntrada, r.HoraEntrada, r.FechaSalida, r.HoraSalida })
                .ToListAsync();

            foreach (var log in others)
            {
                var eEntry = log.FechaEntrada.ToDateTime(log.HoraEntrada);
                var eExit = log.FechaSalida?.ToDateTime(log.HoraSalida ?? default);

                bool conflict = false;

                if (exitDateTime.HasValue && eExit.HasValue)
                    conflict = entryDateTime < eExit && exitDateTime > eEntry;
                else if (!exitDateTime.HasValue && !eExit.HasValue)
                    conflict = true;
                else if (!exitDateTime.HasValue && eExit.HasValue)
                    conflict = entryDateTime < eExit;
                else if (exitDateTime.HasValue && !eExit.HasValue)
                    conflict = eEntry < exitDateTime;

                if (conflict)
                    return ServiceResult.FailureResult($"Conflicto temporal con registro existente (ID: {log.Id}).");
            }

            // Actualizar
            existing.FechaEntrada = entryDate;
            existing.HoraEntrada = entryTime;
            existing.FechaSalida = exitDate;
            existing.HoraSalida = exitTime;
            existing.IndicadorEntrada = true;
            existing.IndicadorSalida = exitDate.HasValue && exitTime.HasValue;
            existing.RegistroUsuarioId = currentUserId;

            // refrescar nombres desde maestro (opcional)
            var emp = await _context.AdmEmpleados
                .Include(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(e => e.Sucursal)
                .FirstOrDefaultAsync(e => e.CodCedula == vm.EmployeeId);

            if (emp != null)
            {
                existing.PrimerNombreEmpleado = emp.PrimerNombre;
                existing.SegundoNombreEmpleado = emp.SegundoNombre;
                existing.PrimerApellidoEmpleado = emp.PrimerApellido;
                existing.SegundoApellidoEmpleado = emp.SegundoApellido;
                existing.NombreCargoEmpleado = emp.Cargo?.NombreCargo;
                existing.NombreUnidadEmpleado = emp.Cargo?.Unidad?.NombreUnidad;
                existing.NombreSucursalEmpleado = emp.Sucursal?.NombreSucursal;
            }

            try
            {
                _context.SegRegistroEmpleados.Update(existing);
                await _context.SaveChangesAsync();

                if (existing.IndicadorSalida && existing.FechaSalida.HasValue && existing.HoraSalida.HasValue)
                    await UpdateDailyRouteJTLogAsync(existing.CodCedula, existing.FechaSalida.Value, existing.HoraSalida.Value, false, currentUserId);

                return ServiceResult.SuccessResult("Registro actualizado exitosamente.", vm.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DB error updating log ID {Id}", vm.Id);
                return ServiceResult.FailureResult("Error al actualizar el registro.");
            }
        }

        public async Task<ServiceResult> RecordManualEmployeeExitAsync(EmployeeLogManualExitViewModel vm, string currentUserId, string? confirmedValidation)
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

            var unitType = existing.Empleado?.Cargo?.Unidad?.TipoUnidad;

            var entryDT = existing.FechaEntrada.ToDateTime(existing.HoraEntrada);
            var exitDT = vm.ExitDate.Value.ToDateTime(vm.ExitTime.Value);

            if (unitType == "A" && vm.ExitDate.Value != existing.FechaEntrada)
                return ServiceResult.FailureResult("Para tipo 'A', la fecha de salida debe ser igual a la de entrada.");

            if (exitDT <= entryDT)
                return ServiceResult.FailureResult("La salida debe ser posterior a la entrada.");

            if (exitDT > now.AddMinutes(5))
                return ServiceResult.FailureResult("La salida no puede ser futura.");

            var worked = exitDT - entryDT;
            var hours = worked.TotalHours;

            if (hours <= 0) return ServiceResult.FailureResult("Horas trabajadas deben ser positivas.");
            if (hours > 23) return ServiceResult.FailureResult($"No puede exceder 23 horas continuas. ({hours:F2})");
            if (worked.TotalDays > 3) return ServiceResult.FailureResult("Un turno no puede exceder 3 días consecutivos.");

            if ((hours < 4 || hours > 20) &&
                (confirmedValidation ?? vm.ConfirmedValidation) is not ("minHours" or "maxHours"))
            {
                var validationType = hours < 4 ? "minHours" : "maxHours";
                var msg = hours < 4
                    ? $"El empleado debe trabajar mínimo 4 horas. Horas registradas: {hours:F2}"
                    : $"El empleado no puede trabajar más de 20 horas continuas. Horas registradas: {hours:F2}";
                return ServiceResult.ConfirmationRequired(msg, validationType, hours);
            }

            existing.FechaSalida = vm.ExitDate;
            existing.HoraSalida = vm.ExitTime;
            existing.IndicadorSalida = true;
            existing.RegistroUsuarioId = currentUserId;

            try
            {
                _context.SegRegistroEmpleados.Update(existing);
                await _context.SaveChangesAsync();
                await UpdateDailyRouteJTLogAsync(existing.CodCedula, vm.ExitDate.Value, vm.ExitTime.Value, false, currentUserId);

                return ServiceResult.SuccessResult("Salida registrada exitosamente.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DB error recording manual exit ID {Id}", vm.Id);
                return ServiceResult.FailureResult("Error al registrar la salida manual: " + ex.Message);
            }
        }

        // =========================================================
        // CONSULTAS / REPORTES
        // =========================================================
        public async Task<EmployeeLogStateDto> GetEmployeeLogStatusAsync(int employeeId)
        {
            var now = DateTime.Now;
            var currentDate = DateOnly.FromDateTime(now.Date);
            var currentTime = TimeOnly.FromDateTime(now);

            var employee = await _context.AdmEmpleados
                .Include(e => e.Cargo).ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(e => e.CodCedula == employeeId);

            if (employee == null)
                return new EmployeeLogStateDto { Status = "error", ErrorMessage = "Empleado no encontrado" };

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
            {
                return new EmployeeLogStateDto
                {
                    Status = "openEntry",
                    EntryDate = openToday.FechaEntrada.ToString("yyyy-MM-dd"),
                    EntryTime = openToday.HoraEntrada.ToString("HH:mm"),
                    CurrentDate = currentDate.ToString("yyyy-MM-dd"),
                    CurrentTime = currentTime.ToString("HH:mm"),
                    UnitType = unitType,
                    OpenLogId = openToday.Id
                };
            }
            if (completeToday != null)
            {
                return new EmployeeLogStateDto
                {
                    Status = "completeEntry",
                    EntryDate = completeToday.FechaEntrada.ToString("yyyy-MM-dd"),
                    EntryTime = completeToday.HoraEntrada.ToString("HH:mm"),
                    ExitDate = completeToday.FechaSalida?.ToString("yyyy-MM-dd"),
                    ExitTime = completeToday.HoraSalida?.ToString("HH:mm"),
                    CurrentDate = currentDate.ToString("yyyy-MM-dd"),
                    CurrentTime = currentTime.ToString("HH:mm"),
                    UnitType = unitType
                };
            }
            if (openYesterday != null)
            {
                return new EmployeeLogStateDto
                {
                    Status = "openEntryYesterday",
                    EntryDate = openYesterday.FechaEntrada.ToString("yyyy-MM-dd"),
                    EntryTime = openYesterday.HoraEntrada.ToString("HH:mm"),
                    CurrentDate = currentDate.ToString("yyyy-MM-dd"),
                    CurrentTime = currentTime.ToString("HH:mm"),
                    UnitType = unitType,
                    OpenLogId = openYesterday.Id
                };
            }

            return new EmployeeLogStateDto
            {
                Status = "noEntry",
                CurrentDate = currentDate.ToString("yyyy-MM-dd"),
                CurrentTime = currentTime.ToString("HH:mm"),
                UnitType = unitType
            };
        }

        public async Task<Tuple<List<EmployeeLogSummaryViewModel>, int>> GetFilteredEmployeeLogsAsync(
            string currentUserId, int? cargoId, string? unitId, int? branchId,
            DateOnly? startDate, DateOnly? endDate, int? logStatus,
            string? search, int page, int pageSize, bool isAdmin)
        {
            List<int> permittedBranchIds;

            if (!isAdmin)
            {
                var claims = await _context.UserClaims
                    .Where(uc => uc.UserId == currentUserId && uc.ClaimType == "SucursalId")
                    .Select(uc => uc.ClaimValue).ToListAsync();

                permittedBranchIds = claims.Where(s => int.TryParse(s, out _)).Select(int.Parse).ToList();
                if (!permittedBranchIds.Any())
                    return Tuple.Create(new List<EmployeeLogSummaryViewModel>(), 0);
            }
            else
            {
                permittedBranchIds = await _context.AdmSucursales.Where(s => s.Estado)
                    .Select(s => s.CodSucursal).ToListAsync();
                if (!permittedBranchIds.Any())
                    return Tuple.Create(new List<EmployeeLogSummaryViewModel>(), 0);
            }

            var (rows, total) = await ExecuteGetFilteredEmployeeLogsSpAsync(
                currentUserId, permittedBranchIds, cargoId, unitId, branchId,
                startDate, endDate, logStatus, search, page, pageSize, isAdmin);

            return Tuple.Create(rows, total);
        }

        /// <summary>
        /// Obtener información de empleados con filtros y permisos.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="permittedBranchIds"></param>
        /// <param name="searchInput"></param>
        /// <param name="isAdmin"></param>
        /// <returns>informacion del empleado</returns>
        public async Task<List<AdmEmpleado>> GetEmployeeInfoAsync(
            string userId, List<int> permittedBranchIds, string? searchInput, bool isAdmin)
        {
            var tvp = new DataTable();
            tvp.Columns.Add("Value", typeof(int));
            foreach (var id in permittedBranchIds) tvp.Rows.Add(id);

            var pUserId = new SqlParameter("@UserId", userId);
            var pPermittedBranchIds = new SqlParameter("@PermittedBranchIds", tvp)
            {
                TypeName = "dbo.IntListType",
                SqlDbType = SqlDbType.Structured
            };
            if (permittedBranchIds.Count == 0) pPermittedBranchIds.Value = DBNull.Value;

            var pSearchInput = new SqlParameter("@SearchInput", string.IsNullOrWhiteSpace(searchInput) ? (object)DBNull.Value : searchInput!);
            var pIsAdmin = new SqlParameter("@IsAdmin", isAdmin);

            var result = new List<AdmEmpleado>();

            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "EXEC dbo.GetEmployeeInfoFiltered @UserId, @PermittedBranchIds, @SearchInput, @IsAdmin";
            cmd.Parameters.AddRange(new[] { pUserId, pPermittedBranchIds, pSearchInput, pIsAdmin });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var emp = new AdmEmpleado
                {
                    CodCedula = reader.GetInt32(reader.GetOrdinal("CodCedula")),
                    PrimerNombre = reader.IsDBNull(reader.GetOrdinal("PrimerNombre")) ? null : reader.GetString(reader.GetOrdinal("PrimerNombre")),
                    SegundoNombre = reader.IsDBNull(reader.GetOrdinal("SegundoNombre")) ? null : reader.GetString(reader.GetOrdinal("SegundoNombre")),
                    PrimerApellido = reader.IsDBNull(reader.GetOrdinal("PrimerApellido")) ? null : reader.GetString(reader.GetOrdinal("PrimerApellido")),
                    SegundoApellido = reader.IsDBNull(reader.GetOrdinal("SegundoApellido")) ? null : reader.GetString(reader.GetOrdinal("SegundoApellido")),
                    NombreCompleto = reader.IsDBNull(reader.GetOrdinal("NombreCompleto")) ? null : reader.GetString(reader.GetOrdinal("NombreCompleto")),
                    FotoUrl = reader.IsDBNull(reader.GetOrdinal("FotoUrl")) ? null : reader.GetString(reader.GetOrdinal("FotoUrl")),
                    CodCargo = reader.IsDBNull(reader.GetOrdinal("CodCargo")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CodCargo")),
                    CodSucursal = reader.IsDBNull(reader.GetOrdinal("CodSucursal")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CodSucursal")),
                    Cargo = new AdmCargo
                    {
                        CodCargo = reader.IsDBNull(reader.GetOrdinal("Cargo_CodCargo")) ? 0 : reader.GetInt32(reader.GetOrdinal("Cargo_CodCargo")),
                        NombreCargo = reader.IsDBNull(reader.GetOrdinal("Cargo_NombreCargo")) ? null : reader.GetString(reader.GetOrdinal("Cargo_NombreCargo")),
                        Unidad = new AdmUnidad
                        {
                            CodUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_CodUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_CodUnidad")),
                            NombreUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_NombreUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_NombreUnidad")),
                            TipoUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_TipoUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_TipoUnidad"))
                        }
                    },
                    Sucursal = new AdmSucursal
                    {
                        CodSucursal = reader.IsDBNull(reader.GetOrdinal("Sucursal_CodSucursal")) ? 0 : reader.GetInt32(reader.GetOrdinal("Sucursal_CodSucursal")),
                        NombreSucursal = reader.IsDBNull(reader.GetOrdinal("Sucursal_NombreSucursal")) ? null : reader.GetString(reader.GetOrdinal("Sucursal_NombreSucursal"))
                    }
                };
                result.Add(emp);
            }
            return result;
        }

        public Task<SegRegistroEmpleado?> GetLogByIdAsync(int id)
            => _context.SegRegistroEmpleados
                .Include(e => e.Empleado).ThenInclude(c => c.Cargo).ThenInclude(u => u.Unidad)
                .Include(e => e.Sucursal)
                .FirstOrDefaultAsync(e => e.Id == id);

        // =========================================================
        // WRAPPERS POR DTO (existen porque tu interfaz los define)
        //     —> SOLO envuelven y llaman a los métodos por ViewModel
        // =========================================================
        public Task<ServiceResult> RecordEmployeeEntryExitAsync(
            EmployeeLogDto dto, string currentUserId, string? confirmedValidation)
        {
            var vm = new EmployeeLogEntryViewModel
            {
                EmployeeId = dto.EmployeeId,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                SecondLastName = dto.SecondLastName,
                CargoId = dto.CargoId,
                CargoName = dto.CargoName,
                UnitId = dto.UnitId,
                UnitName = dto.UnitName,
                // UnitType: el DTO no lo trae; lo resolverán las consultas al maestro si hace falta
                BranchId = dto.BranchId,
                BranchName = dto.BranchName,
                EntryDateStr = dto.EntryDate,
                EntryTimeStr = dto.EntryTime,
                ExitDateStr = dto.ExitDate,
                ExitTimeStr = dto.ExitTime,
                IsEntryRecorded = dto.IsEntryRecorded,
                IsExitRecorded = dto.IsExitRecorded,
                ConfirmedValidation = dto.ConfirmedValidation
            };

            return RecordEmployeeEntryExitAsync(vm, currentUserId, confirmedValidation);
        }

        public Task<ServiceResult> UpdateEmployeeLogAsync(int logId, EmployeeLogDto dto, string currentUserId, string? confirmedValidation)
        {
            // el edit VM usa DateOnly/TimeOnly; parseamos desde dto para llamar al flujo canónico
            DateOnly entryDate = DateOnly.ParseExact(dto.EntryDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            TimeOnly entryTime = TimeOnly.ParseExact(dto.EntryTime!, "HH:mm", CultureInfo.InvariantCulture);
            DateOnly? exitDate = !string.IsNullOrWhiteSpace(dto.ExitDate) ? DateOnly.ParseExact(dto.ExitDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture) : (DateOnly?)null;
            TimeOnly? exitTime = !string.IsNullOrWhiteSpace(dto.ExitTime) ? TimeOnly.ParseExact(dto.ExitTime!, "HH:mm", CultureInfo.InvariantCulture) : (TimeOnly?)null;

            var vm = new EmployeeLogEditViewModel
            {
                Id = logId,
                EmployeeId = dto.EmployeeId,
                CargoId = dto.CargoId,
                CargoName = dto.CargoName,
                UnitId = dto.UnitId,
                UnitName = dto.UnitName,
                BranchId = dto.BranchId,
                BranchName = dto.BranchName,
                EntryDate = entryDate,
                EntryTime = entryTime,
                ExitDate = exitDate,
                ExitTime = exitTime,
                IsEntryRecorded = dto.IsEntryRecorded,
                IsExitRecorded = dto.IsExitRecorded,
                ConfirmedValidation = dto.ConfirmedValidation
            };
            return UpdateEmployeeLogAsync(vm, currentUserId, confirmedValidation);
        }

        public Task<ServiceResult> RecordManualEmployeeExitAsync(int logId, DateOnly exitDate, TimeOnly exitTime, string currentUserId, string? confirmedValidation)
        {
            var vm = new EmployeeLogManualExitViewModel
            {
                Id = logId,
                ExitDate = exitDate,
                ExitTime = exitTime
            };
            return RecordManualEmployeeExitAsync(vm, currentUserId, confirmedValidation);
        }

        // =========================================================
        // HELPERS y acceso a SPs — (copiados de tu código)
        // =========================================================
        private async Task UpdateDailyRouteJTLogAsync(int employeeCedula, DateOnly operationDate, TimeOnly operationTime, bool isEntry, string currentUserId)
        {
            try
            {
                var employee = await _context.AdmEmpleados.Include(e => e.Cargo)
                    .FirstOrDefaultAsync(e => e.CodCedula == employeeCedula);

                if (employee == null || employee.CodCargo != 64)
                {
                    Log.Information("ROUTE_INTEGRATION: Employee {Cedula} is not JT. Not updated.", employeeCedula);
                    return;
                }

                var dailyRouteAssigned = await _context.TdvRutasDiarias
                    .Where(r => r.CedulaJT == employeeCedula && r.FechaEjecucion == operationDate)
                    .FirstOrDefaultAsync();

                if (dailyRouteAssigned == null) return;

                if (isEntry)
                {
                    if (!dailyRouteAssigned.FechaIngresoJT.HasValue)
                    {
                        dailyRouteAssigned.FechaIngresoJT = operationDate;
                        dailyRouteAssigned.HoraIngresoJT = operationTime;
                    }
                }
                else
                {
                    if (!dailyRouteAssigned.FechaSalidaJT.HasValue)
                    {
                        dailyRouteAssigned.FechaSalidaJT = operationDate;
                        dailyRouteAssigned.HoraSalidaJT = operationTime;
                    }
                }

                _context.TdvRutasDiarias.Update(dailyRouteAssigned);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ROUTE_INTEGRATION error JT {Cedula}", employeeCedula);
            }
        }

        private static bool HasChanges(SegRegistroEmpleado data, DateOnly entryDate, TimeOnly entryTime, DateOnly? exitDate, TimeOnly? exitTime)
            => data.FechaEntrada != entryDate ||
               data.HoraEntrada != entryTime ||
               data.FechaSalida != exitDate ||
               data.HoraSalida != exitTime;

        private async Task<List<AdmEmpleado>> GetEmployeeInfo_Core(
            string userId, List<int> permittedBranchIds, string? searchInput, bool isAdmin)
        {
            var tvp = new DataTable();
            tvp.Columns.Add("Value", typeof(int));
            foreach (var id in permittedBranchIds) tvp.Rows.Add(id);

            var pUserId = new SqlParameter("@UserId", userId);
            var pPermittedBranchIds = new SqlParameter("@PermittedBranchIds", tvp)
            {
                TypeName = "dbo.IntListType",
                SqlDbType = SqlDbType.Structured
            };
            if (permittedBranchIds.Count == 0) pPermittedBranchIds.Value = DBNull.Value;

            var pSearchInput = new SqlParameter("@SearchInput", string.IsNullOrWhiteSpace(searchInput) ? (object)DBNull.Value : searchInput!);
            var pIsAdmin = new SqlParameter("@IsAdmin", isAdmin);

            var result = new List<AdmEmpleado>();

            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "EXEC dbo.GetEmployeeInfoFiltered @UserId, @PermittedBranchIds, @SearchInput, @IsAdmin";
            cmd.Parameters.AddRange(new[] { pUserId, pPermittedBranchIds, pSearchInput, pIsAdmin });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var emp = new AdmEmpleado
                {
                    CodCedula = reader.GetInt32(reader.GetOrdinal("CodCedula")),
                    PrimerNombre = reader.IsDBNull(reader.GetOrdinal("PrimerNombre")) ? null : reader.GetString(reader.GetOrdinal("PrimerNombre")),
                    SegundoNombre = reader.IsDBNull(reader.GetOrdinal("SegundoNombre")) ? null : reader.GetString(reader.GetOrdinal("SegundoNombre")),
                    PrimerApellido = reader.IsDBNull(reader.GetOrdinal("PrimerApellido")) ? null : reader.GetString(reader.GetOrdinal("PrimerApellido")),
                    SegundoApellido = reader.IsDBNull(reader.GetOrdinal("SegundoApellido")) ? null : reader.GetString(reader.GetOrdinal("SegundoApellido")),
                    NombreCompleto = reader.IsDBNull(reader.GetOrdinal("NombreCompleto")) ? null : reader.GetString(reader.GetOrdinal("NombreCompleto")),
                    FotoUrl = reader.IsDBNull(reader.GetOrdinal("FotoUrl")) ? null : reader.GetString(reader.GetOrdinal("FotoUrl")),
                    CodCargo = reader.IsDBNull(reader.GetOrdinal("CodCargo")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CodCargo")),
                    CodSucursal = reader.IsDBNull(reader.GetOrdinal("CodSucursal")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CodSucursal")),
                    Cargo = new AdmCargo
                    {
                        CodCargo = reader.IsDBNull(reader.GetOrdinal("Cargo_CodCargo")) ? 0 : reader.GetInt32(reader.GetOrdinal("Cargo_CodCargo")),
                        NombreCargo = reader.IsDBNull(reader.GetOrdinal("Cargo_NombreCargo")) ? null : reader.GetString(reader.GetOrdinal("Cargo_NombreCargo")),
                        Unidad = new AdmUnidad
                        {
                            CodUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_CodUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_CodUnidad")),
                            NombreUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_NombreUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_NombreUnidad")),
                            TipoUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_TipoUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_TipoUnidad"))
                        }
                    },
                    Sucursal = new AdmSucursal
                    {
                        CodSucursal = reader.IsDBNull(reader.GetOrdinal("Sucursal_CodSucursal")) ? 0 : reader.GetInt32(reader.GetOrdinal("Sucursal_CodSucursal")),
                        NombreSucursal = reader.IsDBNull(reader.GetOrdinal("Sucursal_NombreSucursal")) ? null : reader.GetString(reader.GetOrdinal("Sucursal_NombreSucursal"))
                    }
                };
                result.Add(emp);
            }
            return result;
        }

        private async Task<(List<EmployeeLogSummaryViewModel> logs, int totalCount)>
            ExecuteGetFilteredEmployeeLogsSpAsync(
                string userId, List<int> permittedBranchIds, int? cargoId, string? unitId, int? branchIdFilter,
                DateOnly? startDate, DateOnly? endDate, int? logStatus, string? search, int page, int pageSize, bool isAdmin)
        {
            var tvp = new DataTable();
            tvp.Columns.Add("Value", typeof(int));
            foreach (var id in permittedBranchIds) tvp.Rows.Add(id);

            var pUserId = new SqlParameter("@UserId", userId);
            var pPermittedBranchIds = new SqlParameter("@PermittedBranchIds", tvp)
            {
                TypeName = "dbo.IntListType",
                SqlDbType = SqlDbType.Structured
            };
            if (permittedBranchIds.Count == 0) pPermittedBranchIds.Value = DBNull.Value;

            var pCargoId = new SqlParameter("@CargoId", (object?)cargoId ?? DBNull.Value);
            var pUnitId = new SqlParameter("@UnitId", string.IsNullOrWhiteSpace(unitId) ? (object)DBNull.Value : unitId!);
            var pBranchIdFilter = new SqlParameter("@BranchIdFilter", (object?)branchIdFilter ?? DBNull.Value);
            var pStartDate = new SqlParameter("@StartDate", startDate.HasValue ? startDate.Value.ToDateTime(TimeOnly.MinValue) : (object)DBNull.Value);
            var pEndDate = new SqlParameter("@EndDate", endDate.HasValue ? endDate.Value.ToDateTime(TimeOnly.MaxValue) : (object)DBNull.Value);
            var pLogStatus = new SqlParameter("@LogStatus", (object?)logStatus ?? DBNull.Value);
            var pSearchTerm = new SqlParameter("@SearchTerm", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search!);
            var pPage = new SqlParameter("@Page", page);
            var pPageSize = new SqlParameter("@PageSize", pageSize);
            var pIsAdmin = new SqlParameter("@IsAdmin", isAdmin);

            var list = new List<EmployeeLogSummaryViewModel>();
            int totalCount = 0;

            DbConnection conn = _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "EXEC dbo.GetFilteredEmployeeLogs @UserId, @PermittedBranchIds, @CargoId, @UnitId, @BranchIdFilter, @StartDate, @EndDate, @LogStatus, @SearchTerm, @Page, @PageSize, @IsAdmin";

                cmd.Parameters.Add(pUserId);
                cmd.Parameters.Add(pPermittedBranchIds);
                cmd.Parameters.Add(pCargoId);
                cmd.Parameters.Add(pUnitId);
                cmd.Parameters.Add(pBranchIdFilter);
                cmd.Parameters.Add(pStartDate);
                cmd.Parameters.Add(pEndDate);
                cmd.Parameters.Add(pLogStatus);
                cmd.Parameters.Add(pSearchTerm);
                cmd.Parameters.Add(pPage);
                cmd.Parameters.Add(pPageSize);
                cmd.Parameters.Add(pIsAdmin);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                    totalCount = reader.GetInt32(0);

                await reader.NextResultAsync();

                while (await reader.ReadAsync())
                {
                    string primerNombre = reader.IsDBNull(reader.GetOrdinal("PrimerNombreEmpleado"))
                        ? "" : reader.GetString(reader.GetOrdinal("PrimerNombreEmpleado"));
                    string primerApellido = reader.IsDBNull(reader.GetOrdinal("PrimerApellidoEmpleado"))
                        ? "" : reader.GetString(reader.GetOrdinal("PrimerApellidoEmpleado"));
                    string empleadoNombre = $"{primerNombre} {primerApellido}".Trim();

                    var vm = new EmployeeLogSummaryViewModel
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        CodCedula = reader.GetInt32(reader.GetOrdinal("CodCedula")),
                        EmpleadoNombre = empleadoNombre,
                        NombreCargo = reader.IsDBNull(reader.GetOrdinal("NombreCargoEmpleado")) ? null : reader.GetString(reader.GetOrdinal("NombreCargoEmpleado")),
                        NombreUnidad = reader.IsDBNull(reader.GetOrdinal("NombreUnidadEmpleado")) ? null : reader.GetString(reader.GetOrdinal("NombreUnidadEmpleado")),
                        NombreSucursal = reader.IsDBNull(reader.GetOrdinal("NombreSucursalEmpleado")) ? null : reader.GetString(reader.GetOrdinal("NombreSucursalEmpleado")),
                        FechaEntrada = reader.IsDBNull(reader.GetOrdinal("FechaEntrada"))
                            ? DateOnly.MinValue
                            : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("FechaEntrada"))),
                        HoraEntrada = reader.IsDBNull(reader.GetOrdinal("HoraEntrada"))
                            ? TimeOnly.MinValue
                            : TimeOnly.FromTimeSpan(reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("HoraEntrada"))),
                        FechaSalida = reader.IsDBNull(reader.GetOrdinal("FechaSalida"))
                            ? (DateOnly?)null
                            : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("FechaSalida"))),
                        HoraSalida = reader.IsDBNull(reader.GetOrdinal("HoraSalida"))
                            ? (TimeOnly?)null
                            : TimeOnly.FromTimeSpan(reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("HoraSalida"))),
                        IndicadorEntrada = !reader.IsDBNull(reader.GetOrdinal("IndicadorEntrada")) && reader.GetBoolean(reader.GetOrdinal("IndicadorEntrada")),
                        IndicadorSalida = !reader.IsDBNull(reader.GetOrdinal("IndicadorSalida")) && reader.GetBoolean(reader.GetOrdinal("IndicadorSalida")),
                    };
                    list.Add(vm);
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }

            return (list, totalCount);
        }
    }
}