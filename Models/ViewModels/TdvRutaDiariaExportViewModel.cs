using System;
using VCashApp.Enums;

namespace VCashApp.Models.ViewModels
{
    public class TdvRutaDiariaExportViewModel
    {
        // Propiedades de la Ruta Diaria con nombres legibles para exportación
        public string IdRuta { get; set; } // Renombrado de Id
        public string NombreRuta { get; set; }
        public string NombreSucursal { get; set; } // Ya existe en TdvRutaDiaria
        public DateOnly FechaEjecucion { get; set; }
        public string UsuarioPlaneacion { get; set; } // Nombre del usuario, no el ID
        public string TipoRuta { get; set; } // "TRADICIONAL", "ATM", etc.
        public string TipoVehiculo { get; set; } // "CAMIONETA", "BLINDADO", etc.
        public string EstadoRuta { get; set; } // "GENERADO", "PLANEADO", etc.

        // Campos relacionados con el personal y vehículos (si se cargan con Include)
        public string? NombreJT { get; set; }
        public string? NombreConductor { get; set; }
        public string? NombreTripulante { get; set; }
        public string? NombreVehiculo { get; set; } // Asumo que tienes una propiedad para el nombre del vehículo
        public string? NombreCargoJT { get; set; } // Nombre del cargo del JT
        public string? NombreCargoConductor { get; set; } // Nombre del cargo del conductor
        public string? NombreCargoTripulante { get; set; } // Nombre del cargo del tripulante

        // Puedes añadir más campos que necesites, ya resueltos
        public decimal? KmInicial { get; set; }
        public decimal? KmFinal { get; set; }
        public DateOnly? FechaSalidaRuta { get; set; }
        public TimeOnly? HoraSalidaRuta { get; set; }
        public DateOnly? FechaEntradaRuta { get; set; }
        public TimeOnly? HoraEntradaRuta { get; set; }
        public DateOnly? FechaCargue { get; set; }
        public TimeOnly? HoraCargue { get; set; }
        public int? CantBolsaBilleteEntrega { get; set; }
        public int? CantBolsaMonedaEntrega { get; set; }
        public DateOnly? FechaDescargue { get; set; }
        public TimeOnly? HoraDescargue { get; set; }
        public int? CantBolsaBilleteRecibe { get; set; }
        public int? CantBolsaMonedaRecibe { get; set; }
    }
}