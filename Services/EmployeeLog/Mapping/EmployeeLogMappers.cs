using System.Globalization;
using VCashApp.Models.Dtos.EmployeeLog;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.EmployeeLog;

namespace VCashApp.Services.EmployeeLog.Mapping
{
    public static class EmployeeLogMappers
    {
        // ---------- DTO -> VM (ENTRY) ----------
        public static EmployeeLogEntryViewModel ToEntryVm(EmployeeLogDto dto)
        {
            return new EmployeeLogEntryViewModel
            {
                // Empleado
                EmployeeId = dto.EmployeeId,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                SecondLastName = dto.SecondLastName,

                // Catálogos
                CargoId = dto.CargoId ?? 0,
                CargoName = dto.CargoName,
                UnitId = dto.UnitId,
                UnitName = dto.UnitName,
                UnitType = dto.UnitType,
                BranchId = dto.BranchId,
                BranchName = dto.BranchName,

                // Fechas/Horas como string (Service hace parse/validación completa)
                EntryDateStr = dto.EntryDate,
                EntryTimeStr = dto.EntryTime,
                ExitDateStr = dto.ExitDate,
                ExitTimeStr = dto.ExitTime,

                IsEntryRecorded = dto.IsEntryRecorded,
                IsExitRecorded = dto.IsExitRecorded,
                ConfirmedValidation = dto.ConfirmedValidation
            };
        }

        // ---------- DTO -> VM (EDIT/UPDATE) ----------
        public static EmployeeLogEditViewModel ToEditVmFromDto(int logId, EmployeeLogDto dto)
        {
            var entryDate = TryParseDate(dto.EntryDate)
                ?? throw new ArgumentException("La fecha de entrada es obligatoria", nameof(dto.EntryDate));
            var entryTime = TryParseTime(dto.EntryTime)
                ?? throw new ArgumentException("La hora de entrada es obligatoria", nameof(dto.EntryTime));

            return new EmployeeLogEditViewModel
            {
                Id = logId,
                EmployeeId = dto.EmployeeId,
                CargoId = dto.CargoId,
                CargoName = dto.CargoName,
                UnitId = dto.UnitId,
                UnitName = dto.UnitName,
                UnitType = dto.UnitType,
                BranchId = dto.BranchId ?? 0,
                BranchName = dto.BranchName,
                EntryDate = entryDate,
                EntryTime = entryTime,
                ExitDate = TryParseDate(dto.ExitDate),
                ExitTime = TryParseTime(dto.ExitTime),
                IsEntryRecorded = true,
                IsExitRecorded = dto.IsExitRecorded,
                ConfirmedValidation = dto.ConfirmedValidation
            };

            static DateOnly? TryParseDate(string? s)
                => string.IsNullOrWhiteSpace(s) ? null : DateOnly.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            static TimeOnly? TryParseTime(string? s)
                => string.IsNullOrWhiteSpace(s) ? null : TimeOnly.ParseExact(s, "HH:mm", CultureInfo.InvariantCulture);
        }

        // ---------- ENTITY -> VM (EDIT) ----------
        public static EmployeeLogEditViewModel ToEditVm(SegRegistroEmpleado s, bool canEdit)
        {
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
                CanEditLog = canEdit
            };
        }

        // ---------- ENTITY -> VM (DETAILS) ----------
        public static EmployeeLogDetailsViewModel ToDetailsVm(SegRegistroEmpleado s)
        {
            return new EmployeeLogDetailsViewModel
            {
                Id = s.Id,
                EmployeeId = s.CodCedula,
                EmployeeFullName = s.Empleado?.NombreCompleto ?? $"{s.PrimerNombreEmpleado} {s.PrimerApellidoEmpleado}".Trim(),
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

        // ---------- ENTITY -> VM (MANUAL EXIT) ----------
        public static EmployeeLogManualExitViewModel ToManualExitVm(SegRegistroEmpleado s, bool canCreate, bool canEdit)
        {
            return new EmployeeLogManualExitViewModel
            {
                Id = s.Id,
                EmployeeId = s.CodCedula,
                EmployeeFullName = s.Empleado?.NombreCompleto ?? $"{s.PrimerNombreEmpleado} {s.PrimerApellidoEmpleado}".Trim(),
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
                // Sugerencia por defecto
                ExitDate = DateOnly.FromDateTime(DateTime.Now.Date),
                ExitTime = TimeOnly.FromDateTime(DateTime.Now),
                CanCreateLog = canCreate,
                CanEditLog = canEdit
            };
        }

        // ---------- VM -> ENTITY (INSERT) ----------
        public static SegRegistroEmpleado ToNewEntity(
            EmployeeLogEntryViewModel vm,
            DateOnly entryDate, TimeOnly entryTime,
            DateOnly? exitDate, TimeOnly? exitTime,
            int branchId, string currentUserId)
        {
            return new SegRegistroEmpleado
            {
                CodCedula = vm.EmployeeId!.Value,
                PrimerNombreEmpleado = vm.FirstName,
                SegundoNombreEmpleado = vm.MiddleName,
                PrimerApellidoEmpleado = vm.LastName,
                SegundoApellidoEmpleado = vm.SecondLastName,
                CodCargo = vm.CargoId,
                NombreCargoEmpleado = vm.CargoName,
                CodUnidad = vm.UnitId,
                NombreUnidadEmpleado = vm.UnitName,
                CodSucursal = branchId,
                NombreSucursalEmpleado = vm.BranchName,
                FechaEntrada = entryDate,
                HoraEntrada = entryTime,
                FechaSalida = exitDate,
                HoraSalida = exitTime,
                IndicadorEntrada = vm.IsEntryRecorded,
                IndicadorSalida = vm.IsExitRecorded,
                RegistroUsuarioId = currentUserId
            };
        }

        // ---------- REFRESH NAMES ----------
        public static void RefreshNames(SegRegistroEmpleado existing, AdmEmpleado emp)
        {
            existing.PrimerNombreEmpleado = emp.PrimerNombre;
            existing.SegundoNombreEmpleado = emp.SegundoNombre;
            existing.PrimerApellidoEmpleado = emp.PrimerApellido;
            existing.SegundoApellidoEmpleado = emp.SegundoApellido;
            existing.NombreCargoEmpleado = emp.Cargo?.NombreCargo;
            existing.NombreUnidadEmpleado = emp.Cargo?.Unidad?.NombreUnidad;
            existing.NombreSucursalEmpleado = emp.Sucursal?.NombreSucursal;
        }

        // ---------- ENTITY -> DTO (STATUS) ----------
        public static EmployeeLogStatusDto ToStateDto(
            string status, SegRegistroEmpleado log, DateOnly currentDate, TimeOnly currentTime, string? unitType)
        {
            return new EmployeeLogStatusDto
            {
                Status = status,
                EntryDate = log.FechaEntrada.ToString("yyyy-MM-dd"),
                EntryTime = log.HoraEntrada.ToString("HH:mm"),
                ExitDate = log.FechaSalida?.ToString("yyyy-MM-dd"),
                ExitTime = log.HoraSalida?.ToString("HH:mm"),
                CurrentDate = currentDate.ToString("yyyy-MM-dd"),
                CurrentTime = currentTime.ToString("HH:mm"),
                UnitType = unitType,
                OpenLogId = log.Id
            };
        }
    }
}