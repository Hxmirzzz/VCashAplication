using System;
using VCashApp.Enums;

namespace VCashApp.Models.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para exportar la información de una ruta diaria de transporte de valores.
    /// </summary>
    public class TdvRutaDiariaExportViewModel
    {
        public string IdRuta { get; set; }
        public string NombreRuta { get; set; }
        public string NombreSucursal { get; set; }
        public DateOnly FechaEjecucion { get; set; }
        public string UsuarioPlaneacion { get; set; }
        public string TipoRuta { get; set; }
        public string TipoVehiculo { get; set; }
        public string EstadoRuta { get; set; }

        public string? NombreJT { get; set; }
        public string? NombreConductor { get; set; }
        public string? NombreTripulante { get; set; }
        public string? NombreVehiculo { get; set; }
        public string? NombreCargoJT { get; set; }
        public string? NombreCargoConductor { get; set; }
        public string? NombreCargoTripulante { get; set; }

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