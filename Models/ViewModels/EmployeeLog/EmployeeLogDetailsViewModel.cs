namespace VCashApp.Models.ViewModels.EmployeeLog
{
    public class EmployeeLogDetailsViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        // Datos empleado
        public string? EmployeeFullName { get; set; }
        public string? PhotoUrl { get; set; }
        public string? CargoName { get; set; }
        public string? UnitName { get; set; }
        public string? BranchName { get; set; }

        // Tiempos
        public DateOnly EntryDate { get; set; }
        public TimeOnly EntryTime { get; set; }
        public DateOnly? ExitDate { get; set; }
        public TimeOnly? ExitTime { get; set; }

        public int BranchId { get; set; }
    }
}