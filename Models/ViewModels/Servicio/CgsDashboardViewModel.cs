using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels.Servicio
{
    /// <summary>
    /// ViewModel para la página de índice (dashboard) de solicitudes de servicio.
    /// Contiene la lista de solicitudes y las propiedades para filtros y paginación.
    /// </summary>
    public class CgsDashboardViewModel
    {
        public IEnumerable<CgsServiceSummaryViewModel> ServiceRequests { get; set; } = new List<CgsServiceSummaryViewModel>();

        // Propiedades de Paginación y Filtros
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalData { get; set; }
        public string? SearchTerm { get; set; }
        public int? CurrentClientCode { get; set; }
        public int? CurrentBranchCode { get; set; }
        public int? CurrentConceptCode { get; set; }
        public DateOnly? CurrentStartDate { get; set; }
        public DateOnly? CurrentEndDate { get; set; }
        public int? CurrentStatus { get; set; }

        // Propiedades para SelectLists de filtros
        public List<SelectListItem>? AvailableClients { get; set; } = new List<SelectListItem>();
        public List<SelectListItem>? AvailableBranches { get; set; } = new List<SelectListItem>();
        public List<SelectListItem>? AvailableConcepts { get; set; } = new List<SelectListItem>();
        public List<SelectListItem>? AvailableStatuses { get; set; } = new List<SelectListItem>();
    }
}