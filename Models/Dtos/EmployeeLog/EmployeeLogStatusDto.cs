namespace VCashApp.Models.Dtos.EmployeeLog
{
    /// <summary>DTO para el estado del registro de un empleado (usado por el JS).</summary>
    public sealed class EmployeeLogStatusDto
    {
        public string Status { get; set; }
        public string? EntryDate { get; set; }
        public string? EntryTime { get; set; }
        public string? ExitDate { get; set; }
        public string? ExitTime { get; set; }
        public string CurrentDate { get; set; }
        public string CurrentTime { get; set; }
        public string? UnitType { get; set; }
        public string? ErrorMessage { get; set; }
        public double? HoursWorked { get; set; }
        public string? ValidationType { get; set; }
        public bool NeedsConfirmation { get; set; }
        public int? OpenLogId { get; set; }
    }
}
