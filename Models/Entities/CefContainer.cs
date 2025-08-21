using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System;

namespace VCashApp.Models.Entities
{
    [Table("CefContenedores")]
    public class CefContainer
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("IdTransaccionCEF")]
        [ForeignKey("CefTransaction")]
        public int CefTransactionId { get; set; }
        public virtual CefTransaction CefTransaction { get; set; } = null!;

        [Column("IdContenedorPadre")]
        [ForeignKey("ParentContainer")]
        public int? ParentContainerId { get; set; }
        public virtual CefContainer? ParentContainer { get; set; } // Propiedad de navegación al padre
        public virtual ICollection<CefContainer> ChildContainers { get; set; } = new List<CefContainer>(); // Colección de hijos

        [Required]
        [StringLength(50)]
        [Column("TipoContenedor")]
        public string ContainerType { get; set; } // Tipo de contenedor: 'Bolsa', 'Sobre'

        [Required]
        [StringLength(100)]
        [Column("CodigoContenedor")]
        public string ContainerCode { get; set; } // Número de Sello / Número de la Bolsa

        [StringLength(20)]
        [Column("TipoSobre")]
        public string? EnvelopeSubType { get; set; } // null para Bolsa; 'Efectivo' | 'Documento' | 'Cheque' para Sobre

        [Column("ValorDeclarado", TypeName = "DECIMAL(18,0)")]
        public decimal? DeclaredValue { get; set; } // Valor declarado para este contenedor (si aplica)

        [Column("ValorContado", TypeName = "DECIMAL(18,0)")]
        public decimal? CountedValue { get; set; } // Valor contado real, calculado de los detalles

        [Required]
        [StringLength(50)]
        [Column("EstadoContenedor")]
        public string ContainerStatus { get; set; } // Estado: 'Pending', 'InProcess', 'Counted', 'Verified', 'WithIncident'

        [StringLength(255)]
        [Column("Observaciones")]
        public string? Observations { get; set; } // Observaciones específicas del contenedor

        [StringLength(450)]
        [Column("UsuarioProcesamientoId")]
        [ForeignKey("User")]
        public string? ProcessingUserId { get; set; } // Usuario que procesó/contó este contenedor
        //public virtual Usuario ProcessingUserObj { get; set; }

        [Column("FechaProcesamiento", TypeName = "DATETIME")]
        public DateTime? ProcessingDate { get; set; }

        [Column("IdCajeroCliente")]
        public int? ClientCashierId { get; set; }

        [StringLength(255)]
        [Column("NombreCajeroCliente")]
        public string? ClientCashierName { get; set; }

        [Column("FechaSobreCliente", TypeName = "DATE")]
        public DateOnly? ClientEnvelopeDate { get; set; }


        // Colecciones de navegación para las tablas hijas
        public virtual ICollection<CefValueDetail> ValueDetails { get; set; } = new List<CefValueDetail>();
        public virtual ICollection<CefIncident> Incidents { get; set; } = new List<CefIncident>();
    }
}
