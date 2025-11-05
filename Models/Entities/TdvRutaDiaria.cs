using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VCashApp.Models;
using VCashApp.Models.Entities;

namespace VCashApp.Models.Entities
{
    public class TdvRutaDiaria
    {
        [Key]
        public string Id { get; set; }


        // 🟩 Planeador de Ruta (P)
        public string CodRutaSuc { get; set; }
        [ForeignKey("CodRutaSuc")]
        public virtual AdmRoute? RutaMaster { get; set; }

        public string NombreRuta { get; set; }

        public string TipoRuta { get; set; }

        public string TipoVehiculo { get; set; }

        public DateOnly FechaEjecucion { get; set; }

        public int CodSucursal { get; set; }
        [ForeignKey("CodSucursal")]
        public virtual AdmSucursal? Sucursal { get; set; }

        public string NombreSucursal { get; set; }

        public string? CodVehiculo { get; set; }
        [ForeignKey("CodVehiculo")]
        public virtual AdmVehiculo? Vehiculo { get; set; }

        public int? CedulaJT { get; set; }
        [ForeignKey("CedulaJT")]
        public virtual AdmEmpleado? JT { get; set; }

        public string? NombreJT { get; set; }

        public int? CodCargoJT { get; set; }
        [ForeignKey("CodCargoJT")]
        public virtual AdmCargo? CargoJTObj { get; set; }
        public DateOnly? FechaIngresoJT { get; set; }
        public TimeOnly? HoraIngresoJT { get; set; }
        public DateOnly? FechaSalidaJT { get; set; }
        public TimeOnly? HoraSalidaJT { get; set; }


        public int? CedulaConductor { get; set; }
        [ForeignKey("CedulaConductor")]
        public virtual AdmEmpleado? Conductor { get; set; }

        public string? NombreConductor { get; set; }

        public int? CodCargoConductor { get; set; }
        [ForeignKey("CodCargoConductor")]
        public virtual AdmCargo? CargoConductorObj { get; set; }

        public int? CedulaTripulante { get; set; }
        [ForeignKey("CedulaTripulante")]
        public virtual AdmEmpleado? Tripulante { get; set; }

        public string? NombreTripulante { get; set; }

        public int? CodCargoTripulante { get; set; }
        [ForeignKey("CodCargoTripulante")]
        public virtual AdmCargo? CargoTripulanteObj { get; set; }

        public DateOnly FechaPlaneacion { get; set; }

        public TimeOnly HoraPlaneacion { get; set; }

        public string UsuarioPlaneacion { get; set; }
        [ForeignKey("UsuarioPlaneacion")]
        public virtual ApplicationUser? UsuarioPlaneacionObj { get; set; }


        // 🟨 Centro de Efectivo (C) - Orden consecutivo
        public DateOnly? FechaCargue { get; set; }
        public TimeOnly? HoraCargue { get; set; }
        public int? CantBolsaBilleteEntrega { get; set; }
        public int? CantBolsaMonedaEntrega { get; set; }
        public int? CantPlanillaEntrega { get; set; }
        public string? UsuarioCEFCargue { get; set; }
        [ForeignKey("UsuarioCEFCargue")]
        public virtual ApplicationUser? UsuarioCEFCargueObj { get; set; }


        public DateOnly? FechaDescargue { get; set; }
        public TimeOnly? HoraDescargue { get; set; }
        public int? CantBolsaBilleteRecibe { get; set; }
        public int? CantBolsaMonedaRecibe { get; set; }
        public int? CantPlanillaRecibe { get; set; }
        public string? UsuarioCEFDescargue { get; set; }
        [ForeignKey("UsuarioCEFDescargue")]
        public virtual ApplicationUser? UsuarioCEFDescargueObj { get; set; }


        // 🟦 Supervisor de Ruta (S) - Orden consecutivo
        [Column(TypeName = "NUMERIC(18,0)")]
        public decimal? KmInicial { get; set; }

        public DateOnly? FechaSalidaRuta { get; set; }
        public TimeOnly? HoraSalidaRuta { get; set; }

        public string? UsuarioSupervisorApertura { get; set; }
        [ForeignKey("UsuarioSupervisorApertura")]
        public virtual ApplicationUser? UsuarioSupervisorAperturaObj { get; set; }


        [Column(TypeName = "NUMERIC(18,0)")]
        public decimal? KmFinal { get; set; }

        public DateOnly? FechaEntradaRuta { get; set; }

        public TimeOnly? HoraEntradaRuta { get; set; }

        public string? UsuarioSupervisorCierre { get; set; }
        [ForeignKey("UsuarioSupervisorCierre")]
        public virtual ApplicationUser? UsuarioSupervisorCierreObj { get; set; }

        public int Estado { get; set; }

        //public virtual ICollection<TdvRutaDetallePunto> DetallePuntos { get; set; } = new List<TdvRutaDetallePunto>();
    }
}