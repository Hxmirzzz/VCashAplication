using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    public class AdmFondo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string CodigoFondo { get; set; } = null!;
        public int? CodigoFondoVatco { get; set; }
        public int? CodigoCliente { get; set; }
        [ForeignKey("CodigoCliente")]
        //public virtual AdmCliente? Cliente { get; set; }
        public string? NombreFondo { get; set; }
        public int? CodigoSucurusal { get; set; }
        [ForeignKey("CodigoSucursal")]
        public virtual AdmSucursal? Sucursal { get; set; }
        public int? CodigoCiudad { get; set; }
        [ForeignKey("CodigoCiudad")]
        public virtual AdmCiudad? Ciudad { get; set; }
        public DateOnly? FechaCreacion { get; set; }
        public DateOnly? FechaRetiro { get; set; }
        public string? CodCas4u { get; set; }
        public string? DivisaFondo { get; set; }
        public bool Fondo { get; set; }
    }
}