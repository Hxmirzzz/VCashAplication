using System;
using System.ComponentModel.DataAnnotations;
/*using System.ComponentModel.DataAnnotations.Schema;
using VCashApp.Models; // Para ApplicationUser
using VCashApp.Models.Entities;

namespace VCashApp.Models.Entities
{
    public class TdvRutaDetallePunto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Autoincremental
        public int IdDetallePunto { get; set; }

        public string? IdRutaDiaria { get; set; } // FK a TdvRutasDiarias.Id
        [ForeignKey("IdRutaDiaria")]
        public virtual TdvRutaDiaria? RutaDiaria { get; set; }

        public string? Origen { get; set; }
        [ForeignKey("CodFondo")]
        public virtual AdmFondo? OrigenDetalle { get; set; }
        public string? Destino { get; set; } // FK a AdmPunto.CodPunto
        [ForeignKey("CodPunto")]
        public virtual AdmPunto? DestinoDetalle { get; set; }

        public string? IdServicio { get; set; } // FK a AdmServicio.OrdenServicio
        [ForeignKey("IdServicio")]
        public virtual AdmServicio? Servicio { get; set; }

        public int? OrdenPunto { get; set; }
        public string? NombrePunto { get; set; }
        public int? TipoPunto { get; set; }

        public int? CodSucursal { get; set; } // FK a AdmSucursal.CodSucursal
        [ForeignKey("CodSucursal")]
        public virtual AdmSucursal? SucursalPunto { get; set; }

        public string? TipoAccion { get; set; }

        public DateOnly? FechaLlegadaReal { get; set; }
        public TimeOnly? HoraLlegadaReal { get; set; }
        public DateOnly? FechaSalidaReal { get; set; }
        public TimeOnly? HoraSalidaReal { get; set; }

        public string? UsuarioAtencion { get; set; } // FK a ApplicationUser.Id
        [ForeignKey("UsuarioAtencion")]
        public virtual ApplicationUser? UsuarioAtencionObj { get; set; }

        public string? ObservacionesPunto { get; set; }
    }
}*/