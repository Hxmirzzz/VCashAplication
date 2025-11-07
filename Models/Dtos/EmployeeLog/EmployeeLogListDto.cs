namespace VCashApp.Models.Dtos.EmployeeLog
{
    public sealed class EmployeeLogListDto
    {
        public int Id { get; set; }
        public int CodCedula { get; set; }
        public string? PrimerNombreEmpleado { get; set; }
        public string? SegundoNombreEmpleado { get; set; }
        public string? PrimerApellidoEmpleado { get; set; }
        public string? SegundoApellidoEmpleado { get; set; }
        public string? NombreCompletoEmpleado { get; set; }
        public string? NombreCargoEmpleado { get; set; }
        public string? NombreUnidadEmpleado { get; set; }
        public string? NombreSucursalEmpleado { get; set; }
        public DateOnly FechaEntrada { get; set; }
        public TimeOnly HoraEntrada { get; set; }
        public DateOnly? FechaSalida { get; set; }
        public TimeOnly? HoraSalida { get; set; }
        public bool IndicadorEntrada { get; set; }
        public bool IndicadorSalida { get; set; }
        public string? UsuarioRegistroNombre { get; set; }
    }
}