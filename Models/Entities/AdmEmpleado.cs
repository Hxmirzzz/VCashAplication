using System; // Para DateOnly
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VCashApp.Models;
using VCashApp.Enums;

namespace VCashApp.Models.Entities
{
    public class AdmEmpleado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CodCedula { get; set; }

        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? PrimerNombre { get; set; }
        public string? SegundoNombre { get; set; }
        public string? NombreCompleto { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroCarnet { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public DateOnly? FechaExpedicion { get; set; }
        public string? CiudadExpedicion { get; set; }

        public int? CodCargo { get; set; }
        [ForeignKey("CodCargo")]
        public virtual AdmCargo? Cargo { get; set; }

        public int? CodSucursal { get; set; }
        [ForeignKey("CodSucursal")]
        public virtual AdmSucursal? Sucursal { get; set; }

        public string? Celular { get; set; }
        public string? Direccion { get; set; }
        public string? Correo { get; set; }
        public string? RH { get; set; }
        public string? Genero { get; set; }
        public string? OtroGenero { get; set; }
        public DateOnly? FecVinculacion { get; set; }
        public DateOnly? FecRetiro { get; set; }

        public bool IndicadorCatalogo { get; set; }
        public bool IngresoRepublica { get; set; }
        public bool IngresoAeropuerto { get; set; }

        public string? FotoUrl { get; set; }
        public string? FirmaUrl { get; set; }

        public EstadoEmpleado? EmpleadoEstado { get; set; }
    }
}