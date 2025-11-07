namespace VCashApp.Models.Dtos.EmployeeLog
{
    /// <summary>DTO para crear/actualizar registros (datos que envía el JS).</summary>
    public sealed class EmployeeLogDto
    {
        public int EmployeeId { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? SecondLastName { get; set; }

        public int? CargoId { get; set; }
        public string? CargoName { get; set; }
        public string? UnitId { get; set; }
        public string? UnitName { get; set; }
        public string? UnitType { get; set; }

        public int? BranchId { get; set; }
        public string? BranchName { get; set; }

        // Fechas/horas como string (vienen del input HTML)
        public string EntryDate { get; set; }
        public string EntryTime { get; set; }
        public string? ExitDate { get; set; }
        public string? ExitTime { get; set; }

        public bool IsEntryRecorded { get; set; }
        public bool IsExitRecorded { get; set; }

        // Confirmaciones (reglas de negocio)
        public string? ConfirmedValidation { get; set; }
    }
}