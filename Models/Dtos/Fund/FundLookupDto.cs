using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.Dtos.Fund
{
    public sealed class FundLookupDto
    {
        public IEnumerable<SelectListItem> Clients { get; init; } = [];
        public IEnumerable<SelectListItem> Branches { get; init; } = [];
        public IEnumerable<SelectListItem> Cities { get; init; } = [];

        // Si usas catálogos/enums:
        public IEnumerable<SelectListItem> Currencies { get; init; } = [];
        public IEnumerable<SelectListItem> FundTypes { get; init; } = [];
    }
}
