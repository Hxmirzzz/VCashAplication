using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("AdmFondos")]
    public class AdmFondo
    {
        [Key]
        [Column("CodigoFondo")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(450)]
        public string FundCode { get; set; } = null!;

        [Column("CodigoFondoVatco")]
        public int? VatcoFundCode { get; set; }

        [Column("CodigoCliente")]
        public int? ClientCode { get; set; }
        [ForeignKey("ClientCode")]
        public virtual AdmCliente? Client { get; set; }

        [Column("NombreFondo")]
        [StringLength(255)]
        public string? FundName { get; set; }

        [Column("CodigoSucursal")]
        public int? BranchCode { get; set; }
        [ForeignKey("BranchCode")]
        public virtual AdmSucursal? Branch { get; set; }

        [Column("CodigoCiudad")]
        public int? CityCode { get; set; }
        [ForeignKey("CityCode")]
        public virtual AdmCiudad? City { get; set; }

        [Column("FechaCreacion", TypeName = "DATE")]
        public DateOnly? CreationDate { get; set; }

        [Column("FechaRetiro", TypeName = "DATE")]
        public DateOnly? WithdrawalDate { get; set; }

        [Column("CodCas4u")]
        [StringLength(255)]
        public string? Cas4uCode { get; set; }

        [Column("DivisaFondo")]
        [StringLength(50)]
        public string? FundCurrency { get; set; }

        [Column("TipoFondo")]
        public int? FundType { get; set; }

        [Column("EstadoFondo")]
        public bool FundStatus { get; set; }
    }
}