using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("CgsServicios")]
    public class CgsService
    {
        [Key]
        [Column("OrdenServicio")]
        [StringLength(450)]
        public string ServiceOrderId { get; set; } = null!;

        [Column("NumeroPedido")]
        [StringLength(255)]
        public string? RequestNumber { get; set; }

        [Column("CodCliente")]
        public int ClientCode { get; set; }
        [ForeignKey("ClientCode")]
        public virtual AdmCliente? Client { get; set; }

        [Column("CodOsCliente")]
        [StringLength(255)]
        public string? ClientServiceOrderCode { get; set; }

        [Column("CodSucursal")]
        public int BranchCode { get; set; }
        [ForeignKey("BranchCode")]
        public virtual AdmSucursal? Branch { get; set; }

        [Column("FechaSolicitud", TypeName = "DATE")]
        public DateOnly RequestDate { get; set; }

        [Column("HoraSolicitud", TypeName = "TIME(0)")]
        public TimeOnly RequestTime { get; set; }

        [Column("CodConcepto")]
        public int ConceptCode { get; set; }
        [ForeignKey("ConceptCode")]
        public virtual AdmConcepto? Concept { get; set; }

        [Column("TipoTraslado")]
        [StringLength(1)]
        public string? TransferType { get; set; }

        [Column("CodEstado")]
        public int StatusCode { get; set; }
        [ForeignKey("StatusCode")]
        public virtual AdmState? Status { get; set; }

        [Column("CodFlujo")]
        public int? FlowCode { get; set; }

        // --- Origen ---
        [Column("CodClienteOrigen")]
        [ForeignKey("OriginClientCode")]
        public int? OriginClientCode { get; set; }
        public virtual AdmCliente? OriginClient { get; set; }

        [Required]
        [Column("CodPuntoOrigen")]
        [StringLength(25)]
        public string OriginPointCode { get; set; } = null!;

        [Required]
        [Column("IndicadorTipoOrigen")]
        [StringLength(1)]
        public string OriginIndicatorType { get; set; } = null!; // IndicadorOrigen: 'P' (Punto), 'F' (Fondo)

        // --- Destino ---
        [Column("CodClienteDestino")]
        public int? DestinationClientCode { get; set; }
        [ForeignKey("DestinationClientCode")]
        public virtual AdmCliente? DestinationClient { get; set; }

        [Required]
        [Column("CodPuntoDestino")]
        [StringLength(255)]
        public string DestinationPointCode { get; set; } = null!;

        [Required]
        [Column("IndicadorTipoDestino")]
        [StringLength(1)]
        public string DestinationIndicatorType { get; set; } = null!;

        // --- Fechas y Horas de Flujo ---
        [Column("FechaAceptacion", TypeName = "DATE")]
        public DateOnly? AcceptanceDate { get; set; }
        [Column("HoraAceptacion", TypeName = "TIME(0)")]
        public TimeOnly? AcceptanceTime { get; set; }

        [Column("FechaProgramacion", TypeName = "DATE")]
        public DateOnly? ProgrammingDate { get; set; }
        [Column("HoraProgramacion", TypeName = "TIME(0)")]
        public TimeOnly? ProgrammingTime { get; set; }

        [Column("FechaAtencionInicial", TypeName = "DATE")]
        public DateOnly? InitialAttentionDate { get; set; }
        [Column("HoraAtencionInicial", TypeName = "TIME(0)")]
        public TimeOnly? InitialAttentionTime { get; set; }

        [Column("FechaAtencionFinal", TypeName = "DATE")]
        public DateOnly? FinalAttentionDate { get; set; }
        [Column("HoraAtencionFinal", TypeName = "TIME(0)")]
        public TimeOnly? FinalAttentionTime { get; set; }

        [Column("FechaCancelacion", TypeName = "DATE")]
        public DateOnly? CancellationDate { get; set; }
        [Column("HoraCancelacion", TypeName = "TIME(0)")]
        public TimeOnly? CancellationTime { get; set; }

        [Column("FechaRechazo", TypeName = "DATE")]
        public DateOnly? RejectionDate { get; set; }
        [Column("HoraRechazo", TypeName = "TIME(0)")]
        public TimeOnly? RejectionTime { get; set; }

        [Column("Fallido")]
        public bool IsFailed { get; set; }

        [Column("ResponsableFallido")]
        [StringLength(255)]
        public string? FailedResponsible { get; set; }

        [Column("RazonFallido")]
        [StringLength(450)]
        public string? FailedReason { get; set; }

        [Column("PersonaCancelacion")]
        [StringLength(255)]
        public string? CancellationPerson { get; set; }

        [Column("OperadorCancelacion")]
        [StringLength(255)]
        public string? CancellationOperator { get; set; }

        [Column("ModalidadServicio")]
        [StringLength(1)]
        public string? ServiceModality { get; set; } // ModalidadServicio: '1' (Programado), '2' (Pedido), '3' (Frecuente)

        [Column("Observaciones")]
        [StringLength(255)]
        public string? Observations { get; set; }

        [Column("Clave")]
        public int? KeyValue { get; set; }

        [Column("OperadorCgsId")]
        [StringLength(450)]
        public string? CgsOperatorId { get; set; }
        [ForeignKey("CgsOperatorId")]
        public virtual ApplicationUser? CgsOperator { get; set; }

        [Column("SucursalCgs")]
        [StringLength(255)]
        public string? CgsBranchName { get; set; }

        [Column("IpOperador")]
        [StringLength(50)]
        public string? OperatorIpAddress { get; set; }

        [Column("ValorBillete", TypeName = "DECIMAL(18,0)")]
        public decimal? BillValue { get; set; }

        [Column("ValorMoneda", TypeName = "DECIMAL(18,0)")]
        public decimal? CoinValue { get; set; }

        [Column("ValorServicio", TypeName = "DECIMAL(18,0)")]
        public decimal? ServiceValue { get; set; }

        [Column("NumeroKitsCambio")]
        public int? NumberOfChangeKits { get; set; }

        [Column("NumeroBolsasMoneda")]
        public int? NumberOfCoinBags { get; set; }

        [Column("MotivoCancelacion")]
        [StringLength(450)]
        public string? CancellationReason { get; set; }

        [Column("ArchivoDetalle")]
        [StringLength(450)]
        public string? DetailFile { get; set; }
    }
}