using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("CefTransacciones")]
    public class CefTransaction
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        [Column("OrdenServicio")]
        [ForeignKey("CgsService")]
        public string ServiceOrderId { get; set; }
        [NotMapped]
        public virtual CgsService Service { get; set; }

        [Column("CodSucursal")]
        [ForeignKey("AdmSucursal")]
        public int? BranchCode { get; set; }
        [NotMapped]
        public virtual AdmSucursal? Branch { get; set; }

        [StringLength(12)]
        [Column("CodRuta")]
        [ForeignKey("TdvRutaDiaria")]
        public string? RouteId { get; set; }
        [NotMapped]
        public virtual TdvRutaDiaria? TdvRutaDiaria { get; set; }

        [Required]
        [Column("NumeroPlanilla")]
        public int SlipNumber { get; set; }

        [Required]
        [StringLength(3)]
        [Column("Divisa")]
        public string Currency { get; set; } // Divisa (ej: 'COP', 'USD')

        [Required]
        [StringLength(50)]
        [Column("TipoTransaccion")]
        public string TransactionType { get; set; } // Tipo de transacción: 'Collection', 'Provision', 'Audit'

        [Column("NumeroMesaConteo")]
        public int? CountingTableNumber { get; set; } // Número de mesa de conteo

        [Required]
        [Column("CantidadBolsasDeclaradas")]
        public int DeclaredBagCount { get; set; }

        [Required]
        [Column("CantidadSobresDeclarados")]
        public int DeclaredEnvelopeCount { get; set; }

        [Required]
        [Column("CantidadChequesDeclarados")]
        public int DeclaredCheckCount { get; set; }

        [Required]
        [Column("CantidadDocumentosDeclarados")]
        public int DeclaredDocumentCount { get; set; }

        [Required]
        [Column("ValorBilletesDeclarado", TypeName = "DECIMAL(18,0)")]
        public decimal DeclaredBillValue { get; set; }

        [Required]
        [Column("ValorMonedasDeclarado", TypeName = "DECIMAL(18,0)")]
        public decimal DeclaredCoinValue { get; set; }

        [Required]
        [Column("ValorDocumentosDeclarado", TypeName = "DECIMAL(18,0)")]
        public decimal DeclaredDocumentValue { get; set; }

        [Required]
        [Column("ValorTotalDeclarado", TypeName = "DECIMAL(18,0)")]
        public decimal TotalDeclaredValue { get; set; }

        [StringLength(255)]
        [Column("ValorTotalDeclaradoLetras")]
        public string? TotalDeclaredValueInWords { get; set; }

        [Column("ValorTotalContado", TypeName = "DECIMAL(18,0)")]
        public decimal TotalCountedValue { get; set; } // Calculado, no se persiste directamente si siempre es la suma de detalles

        [StringLength(255)]
        [Column("ValorTotalContadoLetras")]
        public string? TotalCountedValueInWords { get; set; }

        [Column("DiferenciaValor", TypeName = "DECIMAL(18,0)")]
        public decimal ValueDifference { get; set; } // Calculado

        [StringLength(255)]
        [Column("NovedadInformativa")]
        public string? InformativeIncident { get; set; } // Novedad informativa de la planilla

        [Required]
        [Column("EsCustodia")]
        public bool IsCustody { get; set; } // Indica si la planilla está en custodia

        [Required]
        [Column("EsPuntoAPunto")]
        public bool IsPointToPoint { get; set; } // Indica si la planilla es punto a punto

        [Required]
        [StringLength(50)]
        [Column("EstadoTransaccion")]
        public string TransactionStatus { get; set; }

        [Required]
        [Column("FechaRegistro", TypeName = "DATETIME")]
        public DateTime RegistrationDate { get; set; } // Fecha de registro de la planilla

        [Required]
        [StringLength(450)]
        [Column("UsuarioRegistroId")]
        [ForeignKey("User")]
        public string RegistrationUser { get; set; }
        [NotMapped]
        public virtual ApplicationUser RegistrationUserObj { get; set; }

        [Column("FechaInicioConteo", TypeName = "DATETIME")]
        public DateTime? CountingStartDate { get; set; } // Fecha de inicio del conteo

        [Column("FechaFinConteo", TypeName = "DATETIME")]
        public DateTime? CountingEndDate { get; set; } // Fecha de fin del conteo

        [StringLength(450)]
        [Column("UsuarioConteoBilletesId")]
        [ForeignKey("User")]
        public string? CountingUserBillId { get; set; }
        [NotMapped]
        public virtual ApplicationUser CountingUserBillObj { get; set; }

        [StringLength(450)]
        [Column("UsuarioConteoMonedasId")]
        [ForeignKey("User")]
        public string? CountingUserCoinId { get; set; }
        [NotMapped]
        public virtual ApplicationUser CountingUserCoinObj { get; set; }

        [StringLength(450)]
        [Column("UsuarioRevisorId")]
        [ForeignKey("User")]
        public string? ReviewerUserId { get; set; }
        [NotMapped]
        public virtual ApplicationUser ReviewerUserObj { get; set; }

        [StringLength(450)]
        [Column("UsuarioBovedaId")]
        [ForeignKey("User")]
        public string? VaultUserId { get; set; }
        [NotMapped]
        public virtual ApplicationUser VaultUserObj { get; set; }

        [Column("FechaUltimaActualizacion", TypeName = "DATETIME")]
        public DateTime? LastUpdateDate { get; set; }

        [StringLength(450)]
        [Column("UsuarioUltimaActualizacionId")]
        [ForeignKey("User")]
        public string? LastUpdateUser { get; set; }
        [NotMapped]
        public virtual ApplicationUser LastUpdateUserObj { get; set; }

        [StringLength(50)]
        [Column("IPRegistro")]
        public string? RegistrationIP { get; set; }

        [StringLength(450)]
        [Column("ReponsableEntregaId")]
        [ForeignKey("User")]
        public string? DelivererId { get; set; }
        [NotMapped]
        public virtual ApplicationUser DelivererUser { get; set; }

        [StringLength(450)]
        [Column("ResponsableRecibeId")]
        [ForeignKey("User")]
        public string? ReceiverId { get; set; }
        [NotMapped]
        public virtual ApplicationUser ReceiverUser { get; set; }

        // Colecciones de navegación para las tablas hijas
        public virtual ICollection<CefContainer> Containers { get; set; } = new List<CefContainer>();
        public virtual ICollection<CefIncident> Incidents { get; set; } = new List<CefIncident>();
    }
}
