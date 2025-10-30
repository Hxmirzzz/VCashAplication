using VCashApp.Enums;

namespace VCashApp.Models.DTOs
{
    public class EmpleadoListadoDto
    {
        public int CodCedula { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? PrimerNombre { get; set; }
        public string? SegundoNombre { get; set; }
        public string? NombreCompleto { get; set; }
        public string? NumeroCarnet { get; set; }
        public string? Genero { get; set; }
        public string? Celular { get; set; }
        public DateOnly? FecVinculacion { get; set; }
        public EstadoEmpleado? EmpleadoEstado { get; set; }
        public string? CargoNombre { get; set; }
        public string? SucursalNombre { get; set; }
        public string? UnidadNombre { get; set; }
    }
}