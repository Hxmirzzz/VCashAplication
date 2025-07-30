using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    public class AdmCliente
    {
        [Key]
        public int CodigoCliente { get; set; }
        public int? ClientePrincipal { get; set; }
        public string NombreCliente { get; set; }
        public string RazonSocial { get; set; }
        public string SiglasCliente { get; set; }
        public int TipoDocumento { get; set; }
        public int NumeroDocumento { get; set; }
        public int CodCiudad { get; set; }
        [ForeignKey("CodCiudad")]
        public virtual AdmCiudad Ciudad { get; set; }
        public int? CiudadFacturacion { get; set; }
        public string? Contacto1 { get; set; }
        public string? CargoContacto1 { get; set; }
        public string? Contacto2 { get; set; }
        public string? CargoContacto2 { get; set; }
        public int TipoCliente { get; set; }
        public string? PaginaWeb { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public bool Estado { get; set; }
    }
}
