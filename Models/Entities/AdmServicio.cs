using System; // Para DateOnly, TimeOnly
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace VCashApp.Models.Entities
{
    public class AdmServicio
    {
        [Key]
        public string OrdenServicio { get; set; } // PK

        public string? NumeroPedido { get; set; }

        public int? CodCliente { get; set; } // FK a AdmCliente (si lo incluyes)
        // [ForeignKey("CodCliente")]
        // public virtual AdmCliente? Cliente { get; set; } // Comentar si AdmCliente no está en scope

        public string? CodigoOsCliente { get; set; }

        public int? CodSucursal { get; set; } // FK a AdmSucursal
        [ForeignKey("CodSucursal")]
        public virtual AdmSucursal? Sucursal { get; set; }

        public string? NombreSucursal { get; set; }

        public DateOnly? FechaSolicitud { get; set; }
        public TimeOnly? HoraSolicitud { get; set; }

        public int? CodConcepto { get; set; } // FK a AdmConcepto
        [ForeignKey("CodConcepto")]
        public virtual AdmConcepto? Concepto { get; set; }

        public string? TipoConcepto { get; set; }
        public string? TipoTraslado { get; set; }

        public int? CodEstado { get; set; } // FK a AdmEstado
        [ForeignKey("CodEstado")]
        public virtual AdmEstado? Estado { get; set; }

        public int? CodFlujo { get; set; }

        public int? CodClienteOrigen { get; set; }
        public string? ClienteOrigen { get; set; }
        public string? CodOrigen { get; set; }
        public string? PuntoOrigen { get; set; }
        public string? CodCiudadOrigen { get; set; }
        public string? CodSucursalOrigen { get; set; }
        public string? CodRangoOrigen { get; set; }

        public bool IndicadorOrigenTipo { get; set; } // Renombrado, Mapea a BIT

        public int? CodClienteDestino { get; set; }
        public string? ClienteDestino { get; set; }
        public string? CodDestino { get; set; }
        public string? PuntoDestino { get; set; }
        public string? CodCiudadDestino { get; set; }
        public string? CodSucursalDestino { get; set; }
        public string? CodRangoDestino { get; set; }
        public bool IndicadorDestinoTipo { get; set; }

        public DateOnly? FechaAceptacion { get; set; }
        public TimeOnly? HoraAceptacion { get; set; }
        public DateOnly? FechaProgramacion { get; set; }
        public TimeOnly? HoraProgramacion { get; set; }
        public DateOnly? FechaAtencionInicial { get; set; }
        public TimeOnly? HoraAtencionInicial { get; set; }
        public DateOnly? FechaAtencionFinal { get; set; }
        public TimeOnly? HoraAtencionFinal { get; set; }
        public DateOnly? FechaCancelacion { get; set; }
        public TimeOnly? HoraCancelacion { get; set; }
        public DateOnly? FechaRechazo { get; set; }
        public TimeOnly? HoraRechazo { get; set; }

        public bool Fallido { get; set; } // Mapea a BIT
        public string? ResponsableFallido { get; set; }
        public string? PersonaCancelacion { get; set; }
        public string? OperadorCancelacion { get; set; }
        public string? ModalidadServicio { get; set; }
        public string? Observaciones { get; set; }
        public string? Clave { get; set; } // Asumo string
        public string? OperadorCgs { get; set; }
        public string? SucursalCgs { get; set; }
        public string? IpOperador { get; set; }

        [Column(TypeName = "DECIMAL(18,0)")]
        public decimal? ValorServicio { get; set; }

        public int? NumeroKitsCambio { get; set; }
        public int? NumeroBolsasMoneda { get; set; }
        public string? MotivoCancelacion { get; set; }
        public string? ArchivoDetalle { get; set; }

        [Column(TypeName = "DECIMAL(18,0)")]
        public decimal? ValorBillete { get; set; }

        [Column(TypeName = "DECIMAL(18,0)")]
        public decimal? ValorMoneda { get; set; }
    }
}