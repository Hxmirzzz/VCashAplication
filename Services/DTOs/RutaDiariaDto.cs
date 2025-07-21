using System.ComponentModel.DataAnnotations;
using System; // Necesario para DateOnly

namespace VCashApp.Services.DTOs
{
    public class RutaDiariaCreationDto
    {
        [Required(ErrorMessage = "Debe seleccionar una Sucursal.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una Sucursal válida.")]
        public int CodSucursal { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una Ruta Maestra.")]
        public string CodRutaSuc { get; set; }

        [Required(ErrorMessage = "La fecha de ejecución es requerida.")]
        public DateOnly FechaEjecucion { get; set; }
    }

    public class GeneracionRutasDiariasResult
    {
        public int RutasCreadas { get; set; }
        public int RutasOmitidas { get; set; }
        public bool ExitoParcial { get; set; }
        public string Mensaje { get; set; }
    }
}