using Microsoft.EntityFrameworkCore;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.EmployeeLog;
using VCashApp.Services.DTOs;
using VCashApp.Data;

namespace VCashApp.Services.EmployeeLog.Validation
{
    public static class EmployeeLogValidators
    {
        // ---- Entry (crear) ----
        public static ServiceResult? ValidateEntryInputs(EmployeeLogEntryViewModel vm)
        {
            if (vm.EmployeeId is null || vm.EmployeeId <= 0)
                return ServiceResult.FailureResult("Debe indicar la cédula del empleado.");

            if (string.IsNullOrWhiteSpace(vm.EntryDateStr))
                return ServiceResult.FailureResult("La fecha de entrada no es válida o está vacía.");

            if (string.IsNullOrWhiteSpace(vm.EntryTimeStr))
                return ServiceResult.FailureResult("La hora de entrada no es válida o está vacía.");

            return null;
        }

        public static ServiceResult? ValidateEntryTemporalRules(DateOnly entryDate, TimeOnly entryTime, DateTime now)
        {
            var entryDT = entryDate.ToDateTime(entryTime);
            if (entryDT > now.AddMinutes(5))
                return ServiceResult.FailureResult("No se puede registrar una entrada con fecha/hora futura.");

            if ((now - entryDT).TotalDays > 15)
                return ServiceResult.FailureResult("No se puede registrar una entrada con más de 15 días de antigüedad.");

            return null;
        }

        public static async Task<ServiceResult?> ValidateNewEntryConflictsAsync(
            AppDbContext ctx, int employeeId, DateTime entryDT, DateOnly currentDate)
        {
            var existingOpenLogAnyDate = await ctx.SegRegistroEmpleados
                .Where(l => l.CodCedula == employeeId && l.IndicadorEntrada && !l.IndicadorSalida)
                .OrderByDescending(l => l.FechaEntrada).ThenByDescending(l => l.HoraEntrada)
                .FirstOrDefaultAsync();

            if (existingOpenLogAnyDate != null)
                return ServiceResult.FailureResult(
                    $"Este empleado ya tiene una entrada abierta (registrada el {existingOpenLogAnyDate.FechaEntrada:dd/MM/yyyy}).");

            var completeLogToday = await ctx.SegRegistroEmpleados
                .Where(l => l.CodCedula == employeeId &&
                            l.FechaEntrada == currentDate &&
                            l.IndicadorEntrada && l.IndicadorSalida)
                .FirstOrDefaultAsync();

            if (completeLogToday != null)
                return ServiceResult.FailureResult("Ya existe un registro completo hoy. No se permite más de una entrada por día.");

            var ultimoRegistroCompleto = await ctx.SegRegistroEmpleados
                .Where(r => r.CodCedula == employeeId && r.FechaSalida.HasValue && r.HoraSalida.HasValue)
                .OrderByDescending(r => r.FechaSalida).ThenByDescending(r => r.HoraSalida)
                .FirstOrDefaultAsync();

            if (ultimoRegistroCompleto != null)
            {
                var ultimaSalida = ultimoRegistroCompleto.FechaSalida!.Value
                    .ToDateTime(ultimoRegistroCompleto.HoraSalida!.Value);

                if ((entryDT - ultimaSalida).TotalHours < 1)
                    return ServiceResult.FailureResult("Debe haber al menos 1 hora entre la salida anterior y la nueva entrada.");
            }

            return null;
        }

        // ---- Exit vs Entry ----
        public static ServiceResult? ValidateExitAgainstEntry(
            DateTime entryDT, DateTime exitDT, DateTime now, string? confirmedValidation)
        {
            if (exitDT <= entryDT)
                return ServiceResult.FailureResult("La salida debe ser posterior a la entrada.");
            if (exitDT > now.AddMinutes(5))
                return ServiceResult.FailureResult("La fecha/hora de salida no puede ser futura.");

            var worked = exitDT - entryDT;
            var hours = worked.TotalHours;

            if (hours <= 0) return ServiceResult.FailureResult("Horas trabajadas deben ser positivas.");
            if (hours > 23) return ServiceResult.FailureResult($"No puede exceder 23 horas continuas. ({hours:F2})");
            if (worked.TotalDays > 3) return ServiceResult.FailureResult("Un turno no puede exceder 3 días consecutivos.");

            if ((hours < 4 || hours > 20) && (confirmedValidation is not ("minHours" or "maxHours")))
            {
                var validationType = hours < 4 ? "minHours" : "maxHours";
                var msg = hours < 4
                    ? $"El empleado debe trabajar mínimo 4 horas. Horas registradas: {hours:F2}"
                    : $"El empleado no puede trabajar más de 20 horas continuas. Horas registradas: {hours:F2}";
                return ServiceResult.ConfirmationRequired(msg, validationType, hours);
            }

            return null;
        }

        // ---- Edit ----
        public static ServiceResult? ValidateEditEntryTemporalRules(DateTime entryDT, DateTime now)
        {
            if (entryDT > now.AddMinutes(5))
                return ServiceResult.FailureResult("La fecha/hora de entrada no pueden ser futuras.");
            if ((now - entryDT).TotalDays > 15)
                return ServiceResult.FailureResult("No se pueden actualizar entradas con más de 15 días de antigüedad.");
            return null;
        }

        public static bool HasChanges(SegRegistroEmpleado data, DateOnly entryDate, TimeOnly entryTime, DateOnly? exitDate, TimeOnly? exitTime)
            => data.FechaEntrada != entryDate ||
               data.HoraEntrada != entryTime ||
               data.FechaSalida != exitDate ||
               data.HoraSalida != exitTime;

        public static async Task<ServiceResult?> ValidateEditConflictsAsync(
            VCashApp.Data.AppDbContext ctx,
            EmployeeLogEditViewModel vm,
            SegRegistroEmpleado existing)
        {
            var unitType = existing.Empleado?.Cargo?.Unidad?.TipoUnidad;

            var logsSameDayExceptCurrent = await ctx.SegRegistroEmpleados
                .Where(r => r.CodCedula == vm.EmployeeId && r.Id != vm.Id && r.FechaEntrada == vm.EntryDate)
                .ToListAsync();

            if (unitType == "A" && logsSameDayExceptCurrent.Any())
                return ServiceResult.FailureResult($"Un empleado tipo 'A' no puede tener múltiples entradas el {vm.EntryDate:dd/MM/yyyy}.");

            if (unitType == "O" && logsSameDayExceptCurrent.Count >= 2)
                return ServiceResult.FailureResult($"Un empleado tipo 'O' no puede tener más de 2 entradas el {vm.EntryDate:dd/MM/yyyy}.");

            var openLogExcluding = await ctx.SegRegistroEmpleados
                .Where(l => l.CodCedula == vm.EmployeeId && l.Id != vm.Id && l.IndicadorEntrada && !l.IndicadorSalida)
                .FirstOrDefaultAsync();

            var currentDate = DateOnly.FromDateTime(DateTime.Now.Date);
            if (openLogExcluding != null && vm.EntryDate == currentDate)
                return ServiceResult.FailureResult($"Este empleado ya tiene una entrada abierta (ID: {openLogExcluding.Id}).");

            var others = await ctx.SegRegistroEmpleados
                .Where(r => r.CodCedula == vm.EmployeeId && r.Id != vm.Id)
                .Select(r => new { r.Id, r.FechaEntrada, r.HoraEntrada, r.FechaSalida, r.HoraSalida })
                .ToListAsync();

            var entryDT = vm.EntryDate.ToDateTime(vm.EntryTime);
            DateTime? exitDT = (vm.ExitDate.HasValue && vm.ExitTime.HasValue) ? vm.ExitDate.Value.ToDateTime(vm.ExitTime.Value) : null;

            foreach (var log in others)
            {
                var eEntry = log.FechaEntrada.ToDateTime(log.HoraEntrada);
                var eExit = log.FechaSalida?.ToDateTime(log.HoraSalida ?? default);

                bool conflict = false;

                if (exitDT.HasValue && eExit.HasValue)
                    conflict = entryDT < eExit && exitDT > eEntry;
                else if (!exitDT.HasValue && !eExit.HasValue)
                    conflict = true;
                else if (!exitDT.HasValue && eExit.HasValue)
                    conflict = entryDT < eExit;
                else if (exitDT.HasValue && !eExit.HasValue)
                    conflict = eEntry < exitDT;

                if (conflict)
                    return ServiceResult.FailureResult($"Conflicto temporal con registro existente (ID: {log.Id}).");
            }

            return null;
        }
    }
}