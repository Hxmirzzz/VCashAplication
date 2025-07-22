using VCashApp.Data;
using VCashApp.Models;
using VCashApp.Models.Entities;
using VCashApp.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using VCashApp.Services.DTOs;

namespace VCashApp.Services
{
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

        private async Task UpdateDailyRouteJTLogAsync(int employeeCedula, DateOnly operationDate, TimeOnly operationTime, bool isEntry, string currentUserId)
        {
            try
            {
                var employee = await _context.AdmEmpleados.Include(e => e.Cargo).FirstOrDefaultAsync(e => e.CodCedula == employeeCedula);

                if (employee == null || employee.CodCargo != 64)
                {
                    Log.Information("ROUTE_INTEGRATION: Employee {Cedula} is not a Shift Leader (CodCargo != 64). TdvRutasDiarias not updated.", employeeCedula);
                    return;
                }

                var dailyRouteAssigned = await _context.TdvRutasDiarias
                    .Where(r => r.CedulaJT == employeeCedula && r.FechaEjecucion == operationDate)
                    .FirstOrDefaultAsync();

                if (dailyRouteAssigned == null)
                {
                    Log.Information("ROUTE_INTEGRATION: Employee {Cedula} is not assigned as JT to a daily route for {OperationDate:yyyy-MM-dd}. TdvRutasDiarias not updated.", employeeCedula, operationDate);
                    return;
                }

                if (isEntry)
                {
                    if (!dailyRouteAssigned.FechaIngresoJT.HasValue)
                    {
                        dailyRouteAssigned.FechaIngresoJT = operationDate;
                        dailyRouteAssigned.HoraIngresoJT = operationTime;
                        Log.Information("ROUTE_INTEGRATION: Route {RouteId} (JT {Cedula}) - Entry Date/Time updated for JT.", dailyRouteAssigned.Id, employeeCedula);
                    }
                }
                else // Is Exit
                {
                    if (!dailyRouteAssigned.FechaSalidaJT.HasValue)
                    {
                        dailyRouteAssigned.FechaSalidaJT = operationDate;
                        dailyRouteAssigned.HoraSalidaJT = operationTime;
                        Log.Information("ROUTE_INTEGRATION: Route {RouteId} (JT {Cedula}) - Exit Date/Time updated for JT.", dailyRouteAssigned.Id, employeeCedula);
                    }
                }

                _context.TdvRutasDiarias.Update(dailyRouteAssigned);
                await _context.SaveChangesAsync();
                Log.Information("ROUTE_INTEGRATION: TdvRutasDiarias successfully updated for Route {RouteId} (JT {Cedula}).", dailyRouteAssigned.Id, employeeCedula);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ROUTE_INTEGRATION: Error updating TdvRutasDiarias for JT {Cedula} on {OperationDate:yyyy-MM-dd}. Message: {ErrorMessage}",
                    employeeCedula, operationDate, ex.Message);
            }
        }

        public async Task<EmployeeLogStateDto> GetEmployeeLogStatusAsync(int employeeId)
        {
            var currentDateTimeNow = DateTime.Now;
            var currentDateNow = DateOnly.FromDateTime(currentDateTimeNow.Date);
            var currentTimeNow = TimeOnly.FromDateTime(currentDateTimeNow);

            Log.Information("SERVICE: GetEmployeeLogStatusAsync - EmployeeId: {EmployeeId}", employeeId);

            var employee = await _context.AdmEmpleados
                .Include(e => e.Cargo)
                .ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(e => e.CodCedula == employeeId);

            if (employee == null)
            {
                Log.Warning("SERVICE: Employee not found in GetEmployeeLogStatusAsync: {EmployeeId}", employeeId);
                return new EmployeeLogStateDto { Status = "error", ErrorMessage = "Empleado no encontrado" };
            }

            string? employeeUnitType = employee.Cargo?.Unidad?.TipoUnidad;

            var openEntryToday = await _context.SegRegistroEmpleados
                .Where(log => log.CodCedula == employeeId &&
                              log.FechaEntrada == currentDateNow &&
                              log.IndicadorEntrada == true &&
                              log.IndicadorSalida == false)
                .FirstOrDefaultAsync();

            var completeEntryToday = await _context.SegRegistroEmpleados
                .Where(log => log.CodCedula == employeeId &&
                              log.FechaEntrada == currentDateNow &&
                              log.IndicadorEntrada == true &&
                              log.IndicadorSalida == true)
                .FirstOrDefaultAsync();

            SegRegistroEmpleado? openEntryYesterday = null;
            if (employeeUnitType == "O")
            {
                openEntryYesterday = await _context.SegRegistroEmpleados
                    .Where(log => log.CodCedula == employeeId &&
                                  log.FechaEntrada == currentDateNow.AddDays(-1) &&
                                  log.IndicadorEntrada == true &&
                                  log.IndicadorSalida == false)
                    .FirstOrDefaultAsync();
            }

            if (openEntryToday != null)
            {
                Log.Information("SERVICE: OPEN ENTRY TODAY - Cedula: {CodCedula} | FechaEntrada: {FechaEntrada} | UnitType: {UnitType}",
                                employeeId, openEntryToday.FechaEntrada.ToString("yyyy-MM-dd"), employeeUnitType);
                return new EmployeeLogStateDto
                {
                    Status = "openEntry",
                    EntryDate = openEntryToday.FechaEntrada.ToString("yyyy-MM-dd"),
                    EntryTime = openEntryToday.HoraEntrada.ToString("HH:mm"),
                    CurrentDate = currentDateNow.ToString("yyyy-MM-dd"),
                    CurrentTime = currentTimeNow.ToString("HH:mm"),
                    UnitType = employeeUnitType,
                    OpenLogId = openEntryToday.Id
                };
            }
            else if (completeEntryToday != null)
            {
                Log.Information("SERVICE: COMPLETE ENTRY TODAY - Cedula: {CodCedula} | FechaEntrada: {FechaEntrada} | UnitType: {UnitType}",
                                employeeId, completeEntryToday.FechaEntrada.ToString("yyyy-MM-dd"), employeeUnitType);
                return new EmployeeLogStateDto
                {
                    Status = "completeEntry",
                    EntryDate = completeEntryToday.FechaEntrada.ToString("yyyy-MM-dd"),
                    EntryTime = completeEntryToday.HoraEntrada.ToString("HH:mm"),
                    ExitDate = completeEntryToday.FechaSalida?.ToString("yyyy-MM-dd"),
                    ExitTime = completeEntryToday.HoraSalida?.ToString("HH:mm"),
                    CurrentDate = currentDateNow.ToString("yyyy-MM-dd"),
                    CurrentTime = currentTimeNow.ToString("HH:mm"),
                    UnitType = employeeUnitType
                };
            }
            else if (openEntryYesterday != null)
            {
                Log.Information("SERVICE: OPEN ENTRY YESTERDAY (O-Type) - Cedula: {CodCedula} | FechaEntrada: {FechaEntrada}",
                                employeeId, openEntryYesterday.FechaEntrada.ToString("yyyy-MM-dd"));
                return new EmployeeLogStateDto
                {
                    Status = "openEntryYesterday",
                    EntryDate = openEntryYesterday.FechaEntrada.ToString("yyyy-MM-dd"),
                    EntryTime = openEntryYesterday.HoraEntrada.ToString("HH:mm"),
                    CurrentDate = currentDateNow.ToString("yyyy-MM-dd"),
                    CurrentTime = currentTimeNow.ToString("HH:mm"),
                    UnitType = employeeUnitType,
                    OpenLogId = openEntryYesterday.Id
                };
            }
            else
            {
                Log.Information("SERVICE: NO OPEN ENTRIES - Cedula: {CodCedula} | UnitType: {UnitType}", employeeId, employeeUnitType);
                return new EmployeeLogStateDto
                {
                    Status = "noEntry",
                    CurrentDate = currentDateNow.ToString("yyyy-MM-dd"),
                    CurrentTime = currentTimeNow.ToString("HH:mm"),
                    UnitType = employeeUnitType
                };
            }
        }

        public async Task<ServiceResult> RecordEmployeeEntryExitAsync(
            EmployeeLogDto logDto,
            string currentUserId,
            string? confirmedValidation)
        {
            string entryDateString = logDto.EntryDate?.Trim() ?? string.Empty;
            string entryTimeString = logDto.EntryTime?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(entryDateString))
            {
                Log.Warning("SERVICIO: RecordEmployeeEntryExitAsync - logDto.EntryDate es null o vacío para employeeId={EmployeeId}. Valor String: '{EntryDateString}'", logDto.EmployeeId, logDto.EntryDate);
                return ServiceResult.FailureResult("La fecha de entrada no es válida o está vacía.");
            }
            if (string.IsNullOrEmpty(entryTimeString))
            {
                Log.Warning("SERVICIO: RecordEmployeeEntryExitAsync - logDto.EntryTime es null o vacío para employeeId={EmployeeId}. Valor String: '{EntryTimeString}'", logDto.EmployeeId, logDto.EntryTime);
                return ServiceResult.FailureResult("La hora de entrada no es válida o está vacía.");
            }

            DateOnly parsedEntryDate;
            TimeOnly parsedEntryTime;
            try
            {
                parsedEntryDate = DateOnly.ParseExact(entryDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                parsedEntryTime = TimeOnly.ParseExact(entryTimeString, "HH:mm", CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                Log.Error(ex, "SERVICIO: RecordEmployeeEntryExitAsync - Error de formato al parsear EntryDate='{EntryDateString}' o EntryTime='{EntryTimeString}' para employeeId={EmployeeId}. Formato esperado: yyyy-MM-dd HH:mm", entryDateString, entryTimeString, logDto.EmployeeId);
                return ServiceResult.FailureResult("Error de formato en la fecha u hora de entrada. Asegúrese que sean válidas.");
            }

            var currentDateTimeNow = DateTime.Now;
            var currentDateNow = DateOnly.FromDateTime(currentDateTimeNow.Date);
            var currentTimeNow = TimeOnly.FromDateTime(currentDateTimeNow);

            Log.Information("SERVICE: RecordEmployeeEntryExitAsync - Start. EmployeeId={EmployeeId}, IsEntryRecorded={IsEntry}, IsExitRecorded={IsExit}, Confirmed='{Confirmed}'",
                logDto.EmployeeId, logDto.IsEntryRecorded, logDto.IsExitRecorded, confirmedValidation ?? "NONE");

            var employee = await _context.AdmEmpleados
                .Include(e => e.Cargo)
                .ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(e => e.CodCedula == logDto.EmployeeId);

            if (employee == null) return ServiceResult.FailureResult("Empleado no encontrado.");
            string? employeeUnitType = employee.Cargo?.Unidad?.TipoUnidad;

            DateTime entryDateTimeClient = parsedEntryDate.ToDateTime(parsedEntryTime);
            if (entryDateTimeClient > currentDateTimeNow.AddMinutes(5)) return ServiceResult.FailureResult("No se puede registrar una entrada con fecha y hora futura.");
            if ((currentDateTimeNow - entryDateTimeClient).TotalDays > 15) return ServiceResult.FailureResult("No se pueden registrar entradas con más de 15 días de antigüedad.");

            if (logDto.IsEntryRecorded == true && logDto.IsExitRecorded == true)
            {
                string exitDateString = logDto.ExitDate?.Trim() ?? string.Empty;
                string exitTimeString = logDto.ExitTime?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(exitDateString) || string.IsNullOrEmpty(exitTimeString))
                    return ServiceResult.FailureResult("Debe proporcionar fecha y hora de salida para un registro combinado de entrada y salida.");

                DateOnly parsedExitDate;
                TimeOnly parsedExitTime;
                try
                {
                    parsedExitDate = DateOnly.ParseExact(exitDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    parsedExitTime = TimeOnly.ParseExact(exitTimeString, "HH:mm", CultureInfo.InvariantCulture);
                }
                catch (FormatException ex)
                {
                    Log.Error(ex, "SERVICIO: RecordEmployeeEntryExitAsync - Error de formato al parsear ExitDate='{ExitDateString}' o ExitTime='{ExitTimeString}' para employeeId={EmployeeId}. Formato esperado: yyyy-MM-dd HH:mm", exitDateString, exitTimeString, logDto.EmployeeId);
                    return ServiceResult.FailureResult("Error de formato en la fecha u hora de salida. Asegúrese que sean válidas.");
                }

                DateTime exitDateTimeClient = parsedExitDate.ToDateTime(parsedExitTime);
                if (exitDateTimeClient <= entryDateTimeClient) return ServiceResult.FailureResult("La hora de salida debe ser posterior a la hora de entrada para un registro combinado.");
                if (exitDateTimeClient > currentDateTimeNow.AddMinutes(5)) return ServiceResult.FailureResult("No se puede registrar una salida futura en un registro combinado.");

                TimeSpan workedTimeCombined = exitDateTimeClient - entryDateTimeClient;
                double hoursWorkedCombined = workedTimeCombined.TotalHours;

                if (hoursWorkedCombined <= 0) return ServiceResult.FailureResult("Las horas trabajadas deben ser positivas. Verifique las fechas y horas en registro combinado.");
                if (hoursWorkedCombined > 23) return ServiceResult.FailureResult($"El empleado no puede trabajar más de 23 horas continuas. Horas registradas: {hoursWorkedCombined:F2}");
                if (workedTimeCombined.TotalDays > 3) return ServiceResult.FailureResult("Un turno combinado no puede exceder 3 días consecutivos.");

                if ((hoursWorkedCombined < 4 || hoursWorkedCombined > 20) && confirmedValidation != "minHours" && confirmedValidation != "maxHours")
                {
                    string validationType = (hoursWorkedCombined < 4) ? "minHours" : "maxHours";
                    string msg = (hoursWorkedCombined < 4) ? $"El empleado debe trabajar mínimo 4 horas. Horas registradas: {hoursWorkedCombined:F2}" : $"El empleado no puede trabajar más de 20 horas continuas. Horas registradas: {hoursWorkedCombined:F2}";
                    return ServiceResult.ConfirmationRequired(msg, validationType, hoursWorkedCombined);
                }
            }

            var existingOpenLogAnyDate = await _context.SegRegistroEmpleados
                .Where(log => log.CodCedula == logDto.EmployeeId && log.IndicadorEntrada == true && log.IndicadorSalida == false)
                .OrderByDescending(log => log.FechaEntrada)
                .ThenByDescending(log => log.HoraEntrada)
                .FirstOrDefaultAsync();

            var completeLogToday = await _context.SegRegistroEmpleados
                .Where(log => log.CodCedula == logDto.EmployeeId &&
                              log.FechaEntrada == currentDateNow &&
                              log.IndicadorEntrada == true &&
                              log.IndicadorSalida == true)
                .FirstOrDefaultAsync();

            Log.Information("SERVICE: {EmployeeId} | DB State before action: Global Open Log ID={OpenLogId}, Complete Log Today ID={CompleteLogId}.",
                logDto.EmployeeId, existingOpenLogAnyDate?.Id.ToString() ?? "N/A", completeLogToday?.Id.ToString() ?? "N/A");

            if (existingOpenLogAnyDate != null)
            {
                Log.Warning("SERVICIO: {EmployeeId} | Fallo: Intento de NUEVA ENTRADA (IsEntryRecorded={IsEntry}, IsExitRecorded={IsExit}) cuando ya existe una entrada abierta global ID:{LogId} desde {LogDate}. |",
                    logDto.EmployeeId, logDto.IsEntryRecorded, logDto.IsExitRecorded, existingOpenLogAnyDate.Id, existingOpenLogAnyDate.FechaEntrada.ToString("yyyy-MM-dd"));
                return ServiceResult.FailureResult($"Este empleado ya tiene una entrada abierta (registrada el {existingOpenLogAnyDate.FechaEntrada:dd/MM/yyyy}) que debe ser cerrada antes de registrar una nueva entrada.");
            }
            else if (completeLogToday != null)
            {
                Log.Warning("SERVICIO: {EmployeeId} | Fallo Validación: Intento de NUEVA INSERCIÓN (Entrada o Combinada) cuando ya existe registro COMPLETO hoy ({LogId}). |",
                    logDto.EmployeeId, completeLogToday.Id);
                return ServiceResult.FailureResult("Ya existe un registro completo (entrada y salida) para este empleado hoy. No se permite más de una entrada por día.");
            }
            else
            {
                var ultimoRegistroCompleto = await _context.SegRegistroEmpleados
                    .Where(r => r.CodCedula == logDto.EmployeeId && r.FechaSalida.HasValue && r.HoraSalida.HasValue)
                    .OrderByDescending(r => r.FechaSalida)
                    .ThenByDescending(r => r.HoraSalida)
                    .FirstOrDefaultAsync();

                if (ultimoRegistroCompleto != null)
                {
                    DateTime ultimaSalida = ultimoRegistroCompleto.FechaSalida.Value.ToDateTime(ultimoRegistroCompleto.HoraSalida.Value);
                    if ((entryDateTimeClient - ultimaSalida).TotalHours < 1)
                    {
                        Log.Warning("SERVICIO: {EmployeeId} | Fallo Validación: Entrada muy cercana a salida anterior ({UltimaSalida}). Requiere al menos 1 hora de espacio. |", logDto.EmployeeId, ultimaSalida.ToString("yyyy-MM-dd HH:mm:ss"));
                        return ServiceResult.FailureResult("Debe haber al menos 1 hora entre la salida anterior y la nueva entrada.");
                    }
                }

                var newLog = new SegRegistroEmpleado
                {
                    CodCedula = logDto.EmployeeId,
                    PrimerNombreEmpleado = logDto.FirstName,
                    SegundoNombreEmpleado = logDto.MiddleName,
                    PrimerApellidoEmpleado = logDto.LastName,
                    SegundoApellidoEmpleado = logDto.SecondLastName,
                    CodCargo = logDto.CargoId,
                    NombreCargoEmpleado = logDto.CargoName,
                    CodUnidad = logDto.UnitId,
                    NombreUnidadEmpleado = logDto.UnitName,
                    CodSucursal = logDto.BranchId,
                    NombreSucursalEmpleado = logDto.BranchName,
                    FechaEntrada = parsedEntryDate,
                    HoraEntrada = parsedEntryTime,
                    FechaSalida = logDto.ExitDate != null ? DateOnly.ParseExact(logDto.ExitDate, "yyyy-MM-dd", CultureInfo.InvariantCulture) : (DateOnly?)null,
                    HoraSalida = logDto.ExitTime != null ? TimeOnly.ParseExact(logDto.ExitTime, "HH:mm", CultureInfo.InvariantCulture) : (TimeOnly?)null,
                    IndicadorEntrada = logDto.IsEntryRecorded,
                    IndicadorSalida = logDto.IsExitRecorded,
                    RegistroUsuarioId = currentUserId
                };

                try
                {
                    await _context.SegRegistroEmpleados.AddAsync(newLog);
                    await _context.SaveChangesAsync();

                    if (newLog.IndicadorEntrada)
                    {
                        await UpdateDailyRouteJTLogAsync(newLog.CodCedula, newLog.FechaEntrada, newLog.HoraEntrada, true, currentUserId);
                    }
                    if (newLog.IndicadorSalida && newLog.FechaSalida.HasValue && newLog.HoraSalida.HasValue)
                    {
                        await UpdateDailyRouteJTLogAsync(newLog.CodCedula, newLog.FechaSalida.Value, newLog.HoraSalida.Value, false, currentUserId);
                    }

                    Log.Information("SERVICE: {EmployeeId} | Result: New log ID:{Id} INSERTED successfully. IndicadorEntrada={IsEntry}, IndicadorSalida={IsExit}.",
                        newLog.CodCedula, newLog.Id, newLog.IndicadorEntrada, newLog.IndicadorSalida);
                    return ServiceResult.SuccessResult("Registro agregado exitosamente.", newLog.Id);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "SERVICIO: Database error saving new log for {EmployeeId}. Message: {ErrorMessage}", logDto.EmployeeId, ex.Message);
                    return ServiceResult.FailureResult("Error al guardar el registro: " + ex.Message);
                }
            }
        }

        public async Task<ServiceResult> UpdateEmployeeLogAsync(
            int logId,
            EmployeeLogDto logDto,
            string currentUserId,
            string? confirmedValidation)
        {
            string entryDateString = logDto.EntryDate?.Trim() ?? string.Empty;
            string entryTimeString = logDto.EntryTime?.Trim() ?? string.Empty;
            string? exitDateString = logDto.ExitDate?.Trim();
            string? exitTimeString = logDto.ExitTime?.Trim();

            Log.Information("SERVICIO: UpdateEmployeeLogAsync - Start. ID={Id}, EmployeeId={EmployeeId}, Confirmed='{Confirmed}'",
                logId, logDto.EmployeeId, confirmedValidation ?? "NONE");
            Log.Information("SERVICIO: UpdateEmployeeLogAsync - Valores recibidos (TRIMMED): EntryDate='{EntryDate}', EntryTime='{EntryTime}', ExitDate='{ExitDate}', ExitTime='{ExitTime}'",
                entryDateString ?? "NULL", entryTimeString ?? "NULL", exitDateString ?? "NULL", exitTimeString ?? "NULL");


            if (string.IsNullOrEmpty(entryDateString))
            {
                Log.Warning("SERVICIO: UpdateEmployeeLogAsync - logDto.EntryDate es null o vacío para logId={LogId}. Valor String: '{EntryDateString}'", logId, logDto.EntryDate);
                return ServiceResult.FailureResult("La fecha de entrada no es válida o está vacía.");
            }
            if (string.IsNullOrEmpty(entryTimeString))
            {
                Log.Warning("SERVICIO: UpdateEmployeeLogAsync - logDto.EntryTime es null o vacío para logId={LogId}. Valor String: '{EntryTimeString}'", logId, logDto.EntryTime);
                return ServiceResult.FailureResult("La hora de entrada no es válida o está vacía.");
            }

            DateOnly parsedEntryDate;
            TimeOnly parsedEntryTime;
            try
            {
                parsedEntryDate = DateOnly.ParseExact(entryDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                parsedEntryTime = TimeOnly.ParseExact(entryTimeString, "HH:mm", CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                Log.Error(ex, "SERVICIO: UpdateEmployeeLogAsync - Error de formato al parsear EntryDate='{EntryDateString}' o EntryTime='{EntryTimeString}' para logId={LogId}. Formato esperado: yyyy-MM-dd HH:mm", entryDateString, entryTimeString, logId);
                return ServiceResult.FailureResult("Error de formato en la fecha u hora de entrada. Asegúrese que sean válidas.");
            }

            var currentDateTimeNow = DateTime.Now;
            var currentDateNow = DateOnly.FromDateTime(currentDateTimeNow.Date);
            var currentTimeNow = TimeOnly.FromDateTime(currentDateTimeNow);

            var existingLog = await _context.SegRegistroEmpleados
                .Include(r => r.Empleado)
                    .ThenInclude(e => e.Cargo)
                        .ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(r => r.Id == logId);

            if (existingLog == null) return ServiceResult.FailureResult("Registro no encontrado para actualización.");
            string? employeeUnitType = existingLog.Empleado?.Cargo?.Unidad?.TipoUnidad;

            DateTime entryDateTime = parsedEntryDate.ToDateTime(parsedEntryTime);
            if (entryDateTime > currentDateTimeNow.AddMinutes(5)) return ServiceResult.FailureResult("La fecha y hora de entrada no pueden ser posteriores a la fecha y hora actuales.");
            if ((currentDateTimeNow - entryDateTime).TotalDays > 15) return ServiceResult.FailureResult("No se pueden actualizar entradas con más de 15 días de antigüedad.");

            DateOnly? parsedExitDate = null;
            TimeOnly? parsedExitTime = null;
            DateTime? exitDateTime = null;

            if ((!string.IsNullOrEmpty(exitDateString) && string.IsNullOrEmpty(exitTimeString)) ||
                (string.IsNullOrEmpty(exitDateString) && !string.IsNullOrEmpty(exitTimeString)))
            {
                return ServiceResult.FailureResult("Si especifica fecha de salida, debe especificar también la hora de salida, y viceversa.");
            }

            if (!string.IsNullOrEmpty(exitDateString) && !string.IsNullOrEmpty(exitTimeString))
            {
                try
                {
                    parsedExitDate = DateOnly.ParseExact(exitDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    parsedExitTime = TimeOnly.ParseExact(exitTimeString, "HH:mm", CultureInfo.InvariantCulture);
                }
                catch (FormatException ex)
                {
                    Log.Error(ex, "SERVICIO: UpdateEmployeeLogAsync - Error de formato al parsear ExitDate='{ExitDateString}' o ExitTime='{ExitTimeString}' para logId={LogId}. Formato esperado: yyyy-MM-dd HH:mm", exitDateString, exitTimeString, logId);
                    return ServiceResult.FailureResult("Error de formato en la fecha u hora de salida. Asegúrese que sean válidas.");
                }

                exitDateTime = parsedExitDate.Value.ToDateTime(parsedExitTime.Value);

                if (exitDateTime <= entryDateTime) return ServiceResult.FailureResult("La fecha y hora de salida deben ser posteriores a la fecha y hora de entrada.");
                if (exitDateTime > currentDateTimeNow.AddMinutes(5)) return ServiceResult.FailureResult("La fecha y hora de salida no pueden ser posteriores a la fecha y hora actuales.");

                TimeSpan workedTime = exitDateTime.Value - entryDateTime;
                double hoursWorked = workedTime.TotalHours;

                if (hoursWorked <= 0) return ServiceResult.FailureResult("Las horas trabajadas deben ser positivas. Verifique las fechas y horas.");
                if (hoursWorked > 23) return ServiceResult.FailureResult($"El empleado no puede trabajar más de 23 horas continuas. Horas registradas: {hoursWorked:F2}");
                if (workedTime.TotalDays > 3) return ServiceResult.FailureResult("Un turno no puede exceder 3 días consecutivos.");

                if ((hoursWorked < 4 || hoursWorked > 20) && confirmedValidation != "minHours" && confirmedValidation != "maxHours")
                {
                    string validationType = (hoursWorked < 4) ? "minHours" : "maxHours";
                    string msg = (hoursWorked < 4) ? $"El empleado debe trabajar mínimo 4 horas. Horas registradas: {hoursWorked:F2}" : $"El empleado no puede trabajar más de 20 horas continuas. Horas registradas: {hoursWorked:F2}";
                    return ServiceResult.ConfirmationRequired(msg, validationType, hoursWorked);
                }
            }

            if (!HasChanges(existingLog, parsedEntryDate, parsedEntryTime, parsedExitDate, parsedExitTime))
                return ServiceResult.FailureResult("No se realizaron cambios en el registro.");

            var existingLogsForDayExcludingCurrent = await _context.SegRegistroEmpleados
                .Where(r => r.CodCedula == logDto.EmployeeId && r.Id != logId && r.FechaEntrada == parsedEntryDate)
                .ToListAsync();

            if (employeeUnitType == "A")
            {
                if (existingLogsForDayExcludingCurrent.Any()) return ServiceResult.FailureResult($"Un empleado tipo 'A' no puede tener múltiples registros de entrada en el mismo día. Ya existe otro registro para el {parsedEntryDate:dd/MM/yyyy} (ID: {existingLogsForDayExcludingCurrent.First().Id})");
            }
            else if (employeeUnitType == "O")
            {
                if (existingLogsForDayExcludingCurrent.Count >= 2) return ServiceResult.FailureResult($"Un empleado tipo 'O' no puede tener más de 2 registros de entrada en el mismo día. Ya existen {existingLogsForDayExcludingCurrent.Count} registros para el {parsedEntryDate:dd/MM/yyyy} (excluyendo el actual).");
            }

            var openLogAnyDateExcludingCurrent = await _context.SegRegistroEmpleados
                .Where(log => log.CodCedula == logDto.EmployeeId && log.Id != logId && log.IndicadorEntrada == true && log.IndicadorSalida == false)
                .FirstOrDefaultAsync();

            if (openLogAnyDateExcludingCurrent != null && parsedEntryDate == currentDateNow)
                return ServiceResult.FailureResult($"Este empleado ya tiene una entrada abierta (ID: {openLogAnyDateExcludingCurrent.Id}) que debe ser cerrada antes de poder registrar una nueva entrada (incluso al actualizar).");

            var conflictingLogs = await _context.SegRegistroEmpleados
                .Where(r => r.CodCedula == logDto.EmployeeId && r.Id != logId)
                .Select(r => new { r.Id, r.FechaEntrada, r.HoraEntrada, r.FechaSalida, r.HoraSalida })
                .ToListAsync();

            foreach (var log in conflictingLogs)
            {
                var existingEntryDateTime = log.FechaEntrada.ToDateTime(log.HoraEntrada);
                var existingExitDateTime = log.FechaSalida?.ToDateTime(log.HoraSalida.Value);

                bool hasConflict = false;
                string conflictMessage = "";

                if (exitDateTime.HasValue && existingExitDateTime.HasValue)
                {
                    if (entryDateTime < existingExitDateTime && exitDateTime > existingEntryDateTime)
                    {
                        hasConflict = true;
                        conflictMessage = $"El registro actualizado se solapa con un registro existente (ID: {log.Id}). Existente: {log.FechaEntrada:dd/MM/yyyy} {log.HoraEntrada:HH:mm} - {log.FechaSalida:dd/MM/yyyy} {log.HoraSalida:HH:mm}";
                    }
                }
                else if (!exitDateTime.HasValue && !existingExitDateTime.HasValue)
                {
                    hasConflict = true;
                    conflictMessage = $"No puede haber dos registros activos (sin salida) simultáneamente para el mismo empleado. El registro existente ID: {log.Id} está aún abierto.";
                }
                else if (!exitDateTime.HasValue && existingExitDateTime.HasValue)
                {
                    if (entryDateTime < existingExitDateTime)
                    {
                        hasConflict = true;
                        conflictMessage = $"El registro que intenta actualizar inicia antes de que termine un registro existente (ID: {log.Id}). Existente termina: {log.FechaSalida:dd/MM/yyyy} {log.HoraSalida:HH:mm}";
                    }
                }
                else if (exitDateTime.HasValue && !existingExitDateTime.HasValue)
                {
                    if (existingEntryDateTime < exitDateTime)
                    {
                        hasConflict = true;
                        conflictMessage = $"Existe un registro activo (ID: {log.Id}) que se solapa con el registro que intenta actualizar. Registro activo desde: {log.FechaEntrada:dd/MM/yyyy} {log.HoraEntrada:HH:mm}";
                    }
                }

                if (hasConflict)
                {
                    Log.Warning("SERVICIO: {EmployeeId} (ID: {Id}) | Fallo Validación: Conflicto temporal con registro existente ID: {ConflictingLogId}. Mensaje: {Mensaje}. | New: [{NewEntry} - {NewExit}], Existing: [{ExistingEntry} - {ExistingExit}]",
                        logDto.EmployeeId, logId, log.Id, conflictMessage, entryDateTime.ToString("yyyy-MM-dd HH:mm:ss"), exitDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NULL",
                        existingEntryDateTime.ToString("yyyy-MM-dd HH:mm:ss"), existingExitDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NULL");
                    return ServiceResult.FailureResult(conflictMessage);
                }
            }

            existingLog.FechaEntrada = parsedEntryDate;
            existingLog.HoraEntrada = parsedEntryTime;
            existingLog.FechaSalida = parsedExitDate;
            existingLog.HoraSalida = parsedExitTime;
            existingLog.IndicadorEntrada = true;
            existingLog.IndicadorSalida = (parsedExitDate.HasValue && parsedExitTime.HasValue);
            existingLog.RegistroUsuarioId = currentUserId;

            var employeeData = await _context.AdmEmpleados
                                             .Include(e => e.Cargo)
                                             .ThenInclude(c => c.Unidad)
                                             .Include(e => e.Sucursal)
                                             .FirstOrDefaultAsync(e => e.CodCedula == logDto.EmployeeId);
            if (employeeData != null)
            {
                existingLog.PrimerNombreEmpleado = employeeData.PrimerNombre;
                existingLog.SegundoNombreEmpleado = employeeData.SegundoNombre;
                existingLog.PrimerApellidoEmpleado = employeeData.PrimerApellido;
                existingLog.SegundoApellidoEmpleado = employeeData.SegundoApellido;
                existingLog.NombreCargoEmpleado = employeeData.Cargo?.NombreCargo;
                existingLog.NombreUnidadEmpleado = employeeData.Cargo?.Unidad?.NombreUnidad;
                existingLog.NombreSucursalEmpleado = employeeData.Sucursal?.NombreSucursal;
            }

            try
            {
                _context.SegRegistroEmpleados.Update(existingLog);
                await _context.SaveChangesAsync();

                if (existingLog.IndicadorSalida && existingLog.FechaSalida.HasValue && existingLog.HoraSalida.HasValue)
                    await UpdateDailyRouteJTLogAsync(existingLog.CodCedula, existingLog.FechaSalida.Value, existingLog.HoraSalida.Value, false, currentUserId);

                Log.Information("SERVICIO: {EmployeeId} (ID: {Id}) | Result: Log entry UPDATED successfully.", logDto.EmployeeId, logId);
                return ServiceResult.SuccessResult("Registro actualizado exitosamente.", logId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SERVICIO: Database error updating log ID {Id} for {EmployeeId}. Message: {ErrorMessage}",
                    logId, logDto.EmployeeId, ex.Message);
                return ServiceResult.FailureResult("Error al actualizar el registro. Por favor, inténtelo nuevamente.");
            }
        }

        private bool HasChanges(SegRegistroEmpleado data, DateOnly entryDate, TimeOnly entryTime, DateOnly? exitDate, TimeOnly? exitTime)
        {
            return data.FechaEntrada != entryDate ||
                   data.HoraEntrada != entryTime ||
                   data.FechaSalida != exitDate ||
                   data.HoraSalida != exitTime;
        }

        public async Task<ServiceResult> RecordManualEmployeeExitAsync(
            int logId,
            DateOnly exitDate,
            TimeOnly exitTime,
            string currentUserId,
            string? confirmedValidation)
        {
            var currentDateTimeNow = DateTime.Now;
            var currentDateNow = DateOnly.FromDateTime(currentDateTimeNow.Date);
            var currentTimeNow = TimeOnly.FromDateTime(currentDateTimeNow);

            Log.Information("SERVICIO: RegistrarSalidaManualAsync - Start. ID={Id}, ExitDate={ExitDate}, ExitTime={ExitTime}, Confirmed='{Confirmed}'",
                logId, exitDate.ToString("yyyy-MM-dd"), exitTime.ToString("HH:mm:ss"), confirmedValidation ?? "NONE");

            var existingLog = await _context.SegRegistroEmpleados
                .Include(r => r.Empleado)
                    .ThenInclude(e => e.Cargo)
                        .ThenInclude(c => c.Unidad)
                .FirstOrDefaultAsync(r => r.Id == logId);

            if (existingLog == null) return ServiceResult.FailureResult("Registro no encontrado.");

            if (existingLog.IndicadorSalida == true && existingLog.FechaSalida.HasValue && existingLog.HoraSalida.HasValue)
                return ServiceResult.FailureResult("Este registro ya tiene una salida asociada.");

            string? employeeUnitType = existingLog.Empleado?.Cargo?.Unidad?.TipoUnidad;
            DateTime entryDateTime = existingLog.FechaEntrada.ToDateTime(existingLog.HoraEntrada);
            DateTime exitDateTime = exitDate.ToDateTime(exitTime);

            if (employeeUnitType == "A" && exitDate != existingLog.FechaEntrada)
                return ServiceResult.FailureResult("La fecha de salida debe ser igual a la de entrada para empleados tipo 'A'.");

            if (exitDateTime <= entryDateTime)
                return ServiceResult.FailureResult("La fecha y hora de salida deben ser posteriores a la fecha y hora de entrada.");

            if (exitDateTime > currentDateTimeNow.AddMinutes(5))
                return ServiceResult.FailureResult("La fecha y hora de salida no pueden ser posteriores a la fecha y hora actuales.");

            TimeSpan workedTime = exitDateTime - entryDateTime;
            double hoursWorked = workedTime.TotalHours;

            if (hoursWorked <= 0) return ServiceResult.FailureResult("Las horas trabajadas deben ser positivas. Verifique las fechas y horas.");
            if (hoursWorked > 23) return ServiceResult.FailureResult($"El empleado no puede trabajar más de 23 horas continuas. Horas registradas: {hoursWorked:F2}");
            if (workedTime.TotalDays > 3) return ServiceResult.FailureResult("Un turno no puede exceder 3 días consecutivos.");

            if ((hoursWorked < 4 || hoursWorked > 20) && confirmedValidation != "minHours" && confirmedValidation != "maxHours")
            {
                string validationType = (hoursWorked < 4) ? "minHours" : "maxHours";
                string msg = (hoursWorked < 4) ? $"El empleado debe trabajar mínimo 4 horas. Horas registradas: {hoursWorked:F2}" : $"El empleado no puede trabajar más de 20 horas continuas. Horas registradas: {hoursWorked:F2}";
                return ServiceResult.ConfirmationRequired(msg, validationType, hoursWorked);
            }

            existingLog.FechaSalida = exitDate;
            existingLog.HoraSalida = exitTime;
            existingLog.IndicadorSalida = true;
            existingLog.RegistroUsuarioId = currentUserId;

            try
            {
                _context.SegRegistroEmpleados.Update(existingLog);
                await _context.SaveChangesAsync();
                await UpdateDailyRouteJTLogAsync(existingLog.CodCedula, exitDate, exitTime, false, currentUserId);

                Log.Information("SERVICIO: Manual exit recorded successfully for {EmployeeId} (ID: {Id}).", existingLog.CodCedula, logId);
                return ServiceResult.SuccessResult("Salida registrada exitosamente.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SERVICIO: Database error recording manual exit ID {Id} for {EmployeeId}. Message: {ErrorMessage}",
                    logId, existingLog.CodCedula, ex.Message);
                return ServiceResult.FailureResult("Error al registrar la salida manual: " + ex.Message);
            }
        }

        public async Task<Tuple<List<SegRegistroEmpleado>, int>> GetFilteredEmployeeLogsAsync(
            string currentUserId,
            int? cargoId,
            string? unitId,
            int? branchId,
            DateOnly? startDate,
            DateOnly? endDate,
            int? logStatus,
            string? search,
            int page,
            int pageSize,
            bool isAdmin)
        {
            List<int> permittedBranchIds = new List<int>();

            if (!isAdmin)
            {
                var userClaims = await _context.UserClaims
                                               .Where(uc => uc.UserId == currentUserId && uc.ClaimType == "SucursalId")
                                               .Select(uc => uc.ClaimValue)
                                               .ToListAsync();

                var validBranchIdStrings = userClaims.Where(s => int.TryParse(s, out int tempVal)).ToList();
                permittedBranchIds = validBranchIdStrings.Select(s => int.Parse(s)).ToList();

                if (!permittedBranchIds.Any())
                {
                    Log.Warning("SERVICE: User {UserId} is not admin and has no SucursalId claims. Returning empty log list.", currentUserId);
                    return Tuple.Create(new List<SegRegistroEmpleado>(), 0);
                }
            }
            else // Si es administrador
            {
                permittedBranchIds = await _context.AdmSucursales.Where(s => s.Estado == true).Select(s => s.CodSucursal).ToListAsync();

                if (!permittedBranchIds.Any())
                {
                    Log.Warning("SERVICE: Admin user {UserId} is trying to access logs, but no active branches found in the system.", currentUserId);
                    return Tuple.Create(new List<SegRegistroEmpleado>(), 0);
                }
            }

            var (logs, totalCount) = await _context.GetFilteredEmployeeLogsFromSpAsync(
                currentUserId,
                permittedBranchIds,
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

            return Tuple.Create(logs, totalCount);
        }
    }
}