using System; // Para DateOnly
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    public class AdmVehiculo
    {
        [Key]
        public string CodVehiculo { get; set; }

        public string? CodigoVatco { get; set; }
        public int? CodSucursal { get; set; }
        [ForeignKey("CodSucursal")]
        public virtual AdmSucursal? Sucursal { get; set; }

        public string? TipoVehiculo { get; set; }
        public decimal? Toneladas { get; set; }
        public string? CodUnidad { get; set; }
        [ForeignKey("CodUnidad")]
        public virtual AdmUnidad? Unidad { get; set; }

        public string? Propiedad { get; set; }
        public string? EmpresaAlquiler { get; set; }
        public string? Color { get; set; }
        public decimal? MontoAutorizado { get; set; }

        public int? ConductorCedula { get; set; }
        [ForeignKey("ConductorCedula")]
        public virtual AdmEmpleado? Conductor { get; set; }

        public string? GPS { get; set; }
        public string? NumeroSoat { get; set; }
        public DateOnly? VencimientoSoat { get; set; }
        public string? NumeroTecnomecanica { get; set; }
        public DateOnly? VencimientoTecnomecanica { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Linea { get; set; }
        public string? Blindaje { get; set; }
        public string? NumeroSerie { get; set; }
        public string? NumeroChasis { get; set; }
        public bool Estado { get; set; }
    }
}