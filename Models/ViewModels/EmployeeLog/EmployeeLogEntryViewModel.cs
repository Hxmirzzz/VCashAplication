namespace VCashApp.Models.ViewModels.EmployeeLog
{
    public class EmployeeLogEntryViewModel
    {
        // Encabezado / permisos
        public string? UserName { get; set; }
        public string PageName { get; set; } = "Crear";
        public string? UnidadName { get; set; }
        public string? BranchName { get; set; }
        public string? FullName { get; set; }
        public bool CanCreateLog { get; set; }
        public bool CanEditLog { get; set; }

        // Prefill opcional
        public int? PrefillEmployeeId { get; set; }
        public string? PrefillEmployeeName { get; set; }
        public string? PrefillPhotoUrl { get; set; }

        // === Datos que el form envía (requeridos por el servicio) ===
        public int? EmployeeId { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? SecondLastName { get; set; }

        public int CargoId { get; set; }
        public string? CargoName { get; set; }
        public string? UnitId { get; set; }
        public string? UnitName { get; set; }
        public string? UnitType { get; set; }
        public int? BranchId { get; set; }

        // Fechas/horas desde inputs HTML (usa string o DateOnly/TimeOnly)
        public string? EntryDateStr { get; set; }
        public string? EntryTimeStr { get; set; }
        public string? ExitDateStr { get; set; }
        public string? ExitTimeStr { get; set; }

        public DateOnly? EntryDate { get; set; }
        public TimeOnly? EntryTime { get; set; }
        public DateOnly? ExitDate { get; set; }
        public TimeOnly? ExitTime { get; set; }

        public bool IsEntryRecorded { get; set; } = true;
        public bool IsExitRecorded { get; set; } = false;

        public string? ConfirmedValidation { get; set; }
    }
}