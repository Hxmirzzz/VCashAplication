using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("CefDetallesValores")]
    public class CefValueDetail
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("IdContenedorCef")]
        [ForeignKey("CefContainer")]
        public int CefContainerId { get; set; }
        public virtual CefContainer CefContainer { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Column("TipoValor")]
        public string ValueType { get; set; }

        [Column("Denominacion")]
        [ForeignKey("AdmDenominacion")]
        public int? DenominationId { get; set; }
        public virtual AdmDenominacion? AdmDenominacion { get; set; } = null!;

        [Column("Calidad")]
        [ForeignKey("AdmQuality")]
        public int? QualityId { get; set; }
        public virtual AdmQuality? AdmQuality { get; set; } = null!;

        [Column("Cantidad")]
        public int? Quantity { get; set; } // Cantidad de unidades (para billetes/monedas)

        [Column("CantidadFajos")]
        public int? BundlesCount { get; set; }

        [Column("CantidadPicos")]
        public int? LoosePiecesCount { get; set; } // Cantidad de picos (para billetes/monedas)

        [Column("ValorUnitario", TypeName = "DECIMAL(18,2)")]
        public decimal? UnitValue { get; set; } // Valor unitario (para cheques/documentos)

        [Required]
        [Column("MontoCalculado", TypeName = "DECIMAL(18,0)")]
        public decimal? CalculatedAmount { get; set; } // Denomination * Quantity o UnitValue

        [Column("EsAltaDenominacion")]
        public bool? IsHighDenomination { get; set; } // Indica si es billete de alta denominación

        [StringLength(100)]
        [Column("NumeroIdentificador")]
        public string? IdentifierNumber { get; set; }

        [StringLength(100)]
        [Column("Banco")]
        public string? BankName { get; set; }

        [Column("FechaEmision", TypeName = "DATE")]
        public DateOnly? IssueDate { get; set; }

        [StringLength(255)]
        [Column("Emisor")]
        public string? Issuer { get; set; } // Emisor (para cheques/documentos)

        [StringLength(255)]
        [Column("Observaciones")]
        public string? Observations { get; set; } // Observaciones específicas del detalle

        public virtual ICollection<CefIncident> Incidents { get; set; } = new List<CefIncident>(); // Novedades relacionadas con este detalle
    }
}
