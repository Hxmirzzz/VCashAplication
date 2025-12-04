using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.Dtos.Point
{
    public sealed class PointLookupDto
    {
        public IEnumerable<SelectListItem> Clientes { get; init; } = [];
        public IEnumerable<SelectListItem> Sucursales { get; init; } = [];
        public IEnumerable<SelectListItem> Ciudades { get; init; } = [];
        public IEnumerable<SelectListItem> Fondos { get; init; } = [];
        public IEnumerable<SelectListItem> Rutas { get; init; } = [];
        public IEnumerable<SelectListItem> Rangos { get; init; } = [];
        public IEnumerable<SelectListItem> TiposNegocio { get; init; } = [];
    }
}
