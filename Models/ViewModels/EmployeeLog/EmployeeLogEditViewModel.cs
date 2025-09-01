namespace VCashApp.Models.ViewModels.EmployeeLog
{
    public class EmployeeLogEditViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        public string? EmployeeFullName { get; set; }
        public string? PhotoUrl { get; set; }
        public int? CargoId { get; set; }
        public string? CargoName { get; set; }
        public string? UnitId { get; set; }
        public string? UnitName { get; set; }
        public string? UnitType { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }

        public DateOnly EntryDate { get; set; }
        public TimeOnly EntryTime { get; set; }
        public DateOnly? ExitDate { get; set; }
        public TimeOnly? ExitTime { get; set; }

        public bool IsEntryRecorded { get; set; } = true;
        public bool IsExitRecorded { get; set; }

        public bool CanEditLog { get; set; }

        // NUEVO:
        public string? ConfirmedValidation { get; set; }

        // (opcional) si editarás nombres desde UI
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? SecondLastName { get; set; }
    }
}
