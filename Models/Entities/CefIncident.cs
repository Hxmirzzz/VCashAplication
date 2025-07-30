using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("CefNovedades")]
    public class CefIncident
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("IdTransaccionCef")]
        [ForeignKey("CefTransaction")]
        public int? CefTransactionId { get; set; }
        public virtual CefTransaction? CefTransaction { get; set; }

        [Column("IdContenedorCef")]
        [ForeignKey("CefContainer")]
        public int? CefContainerId { get; set; }
        public virtual CefContainer? CefContainer { get; set; }

        [Column("IdDetalleValorCef")]
        [ForeignKey("CefValueDetail")]
        public int? CefValueDetailId { get; set; }
        public virtual CefValueDetail? CefValueDetail { get; set; }

        [Required]
        [Column("IdTipoNovedad")]
        [ForeignKey("IncidentType")]
        public int IncidentTypeId { get; set; }
        public virtual CefIncidentType IncidentType { get; set; } = null!;

        [Required]
        [Column("MontoAfectado", TypeName = "DECIMAL(18,0)")]
        public decimal AffectedAmount { get; set; }

        [Column("DenominacionAfectada", TypeName = "DECIMAL(18,0)")]
        public decimal? AffectedDenomination { get; set; }

        [Column("CantidadAfectada")]
        public int? AffectedQuantity { get; set; }

        [Required]
        [StringLength(255)]
        [Column("Descripcion")]
        public string Description { get; set; }

        [Required]
        [StringLength(450)]
        [Column("UsuarioReportaId")]
        [ForeignKey("User")]
        public string ReportedUserId { get; set; }
        // public virtual Usuario ReportedUserObj { get; set; }

        [Required]
        [Column("FechaNovedad", TypeName = "DATETIME")]
        public DateTime IncidentDate { get; set; }

        [Required]
        [StringLength(50)]
        [Column("EstadoNovedad")]
        public string IncidentStatus { get; set; } // Estado: 'Reported', 'UnderReview', 'Adjusted', 'Closed'
    }
}
