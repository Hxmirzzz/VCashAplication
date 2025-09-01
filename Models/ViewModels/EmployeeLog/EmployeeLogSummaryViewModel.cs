using System;

namespace VCashApp.Models.ViewModels.EmployeeLog
{
    public class EmployeeLogSummaryViewModel
    {
        public int Id { get; set; }
        public int CodCedula { get; set; }
        public string? EmpleadoNombre { get; set; } // “PrimerNombre Apellido”
        public string? NombreSucursal { get; set; }
        public string? NombreCargo { get; set; }
        public string? NombreUnidad { get; set; }

        public DateOnly FechaEntrada { get; set; }
        public TimeOnly HoraEntrada { get; set; }
        public DateOnly? FechaSalida { get; set; }
        public TimeOnly? HoraSalida { get; set; }

        public bool IndicadorEntrada { get; set; }
        public bool IndicadorSalida { get; set; }
    }
}
