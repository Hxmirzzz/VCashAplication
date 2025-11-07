namespace VCashApp.Models.Dtos.EmployeeLog
{
    public sealed class EmployeeLogExportDto
    {
        public int CodCedula { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroCarnet { get; set; }
        public string? PrimerNombre { get; set; }
        public string? SegundoNombre { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? NombreCompleto { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public DateOnly? FechaExpedicion { get; set; }
        public string? CiudadExpedicion { get; set; }
        public string? NombreCiudadExpedicion { get; set; }
        public string? CargoNombre { get; set; }
        public string? UnidadNombre { get; set; }
        public string? SucursalNombre { get; set; }
        public string? Celular { get; set; }
        public string? Direccion { get; set; }
        public string? Correo { get; set; }
        public string? BloodType { get; set; }
        public string? Genero { get; set; }
        public string? OtroGenero { get; set; }
        public DateOnly? FechaVinculacion { get; set; }
        public DateOnly? FechaRetiro { get; set; }
        public bool IndicadorCatalogo { get; set; }
        public bool IngresoRepublica { get; set; }
        public bool IngresoAeropuerto { get; set; }
        public int? EmployeeStatus { get; set; }
    }
}
