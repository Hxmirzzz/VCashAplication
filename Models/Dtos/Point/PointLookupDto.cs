using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.Dtos.Point
{
    /// <summary>Datos para los select del formulario de puntos.</summary>
    public sealed class PointLookupDto
    {
        /// <summary>Clientes.</summary>
        public IEnumerable<SelectListItem> Clientes { get; init; } = [];
        /// <summary>Clientes principales.</summary>
        public IEnumerable<SelectListItem> ClientesPrincipales { get; init; } = [];
        /// <summary>Sucursales.</summary>
        public IEnumerable<SelectListItem> Sucursales { get; init; } = [];
        /// <summary>Ciudades.</summary>
        public IEnumerable<SelectListItem> Ciudades { get; init; } = [];
        /// <summary>Fondos.</summary>
        public IEnumerable<SelectListItem> Fondos { get; init; } = [];
        /// <summary>Rutas.</summary>
        public IEnumerable<SelectListItem> Rutas { get; init; } = [];
        /// <summary>Rangos.</summary>
        public IEnumerable<SelectListItem> Rangos { get; init; } = [];
        /// <summary>Tipos de negocio.</summary>
        public IEnumerable<SelectListItem> TiposNegocio { get; init; } = [];
    }
}
