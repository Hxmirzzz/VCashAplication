// En VCashApp.Models.DTOs/EmployeeLogDtos.cs

namespace VCashApp.Models.DTOs
{
    /// <summary>
    /// DTO para listado de registros de empleados (Dashboard e Historial)
    /// </summary>
    public class EmployeeLogListadoDto
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

        // Auditoría
        public string? UsuarioRegistroNombre { get; set; }
    }

    /// <summary>
    /// DTO para búsqueda de empleados (autocomplete en LogEntry)
    /// </summary>
    public class EmpleadoBusquedaDto
    {
        public int CodCedula { get; set; }
        public string? PrimerNombre { get; set; }
        public string? SegundoNombre { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? NombreCompleto { get; set; }
        public int? CodCargo { get; set; }
        public string? CargoNombre { get; set; }
        public string? CodUnidad { get; set; }
        public string? UnidadNombre { get; set; }
        public string? TipoUnidad { get; set; }
        public int? CodSucursal { get; set; }
        public string? SucursalNombre { get; set; }
        public string? FotoUrl { get; set; }
    }
}