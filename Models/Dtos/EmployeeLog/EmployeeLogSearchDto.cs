namespace VCashApp.Models.Dtos.EmployeeLog
{
    public sealed class EmployeeLogSearchDto
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
