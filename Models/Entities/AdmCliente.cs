using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("AdmClientes")]
    public class AdmCliente
    {
        [Key]
        [Column("CodigoCliente")]
        public int ClientCode { get; set; }

        [Column("ClientePrincipal")]
        public int? MainClient { get; set; }

        [Column("NombreCliente")]
        [StringLength(255)]
        public string ClientName { get; set; } = null!;

        [Column("RazonSocial")]
        [StringLength(255)]
        public string BusinessName { get; set; } = null!;

        [Column("SiglasCliente")]
        [StringLength(5)]
        public string ClientAcronym { get; set; } = null!;

        [Column("TipoDocumento")]
        public int DocumentType { get; set; }

        [Column("NumeroDocumento")]
        public int DocumentNumber { get; set; }

        [Column("CodCiudad")]
        public int CityCode { get; set; }
        [ForeignKey("CityCode")]
        public virtual AdmCiudad? City { get; set; }

        [Column("CiudadFacturacion")]
        public int? BillingCity { get; set; }

        [Column("Contacto1")]
        [StringLength(255)]
        public string? Contact1 { get; set; }

        [Column("CargoContacto1")]
        [StringLength(255)]
        public string? PositionContact1 { get; set; }

        [Column("Contacto2")]
        [StringLength(255)]
        public string? Contact2 { get; set; }

        [Column("CargoContacto2")]
        [StringLength(255)]
        public string? PositionContact2 { get; set; }

        [Column("TipoCliente")]
        public int ClientType { get; set; }

        [Column("PaginaWeb")]
        [StringLength(255)]
        public string? Website { get; set; }

        [Column("Telefono")]
        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        [Column("Direccion")]
        [StringLength(255)]
        public string? Address { get; set; }

        [Column("Estado")]
        public bool Status { get; set; } // Estado
    }
}
