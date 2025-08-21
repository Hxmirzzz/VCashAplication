using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels.Servicio
{
    /// <summary>
    /// ViewModel para la página de índice (dashboard) de solicitudes de servicio.
    /// Contiene la lista de solicitudes y las propiedades para filtros y paginación.
    /// </summary>
    public class CgsDashboardViewModel
    {
        /// <summary>
        /// Conjunto de solicitudes de servicio a mostrar.
        /// </summary>
        public IEnumerable<CgsServiceSummaryViewModel> ServiceRequests { get; set; } = new List<CgsServiceSummaryViewModel>();

        /// <summary>
        /// Número de página actual en la paginación.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Cantidad de registros por página.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total de páginas disponibles.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Total de registros encontrados.
        /// </summary>
        public int TotalData { get; set; }

        /// <summary>
        /// Término de búsqueda aplicado al listado.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Código del cliente utilizado como filtro.
        /// </summary>
        public int? CurrentClientCode { get; set; }

        /// <summary>
        /// Código de la sucursal utilizada como filtro.
        /// </summary>
        public int? CurrentBranchCode { get; set; }

        /// <summary>
        /// Código del concepto seleccionado como filtro.
        /// </summary>
        public int? CurrentConceptCode { get; set; }

        /// <summary>
        /// Fecha inicial del rango de búsqueda.
        /// </summary>
        public DateOnly? CurrentStartDate { get; set; }

        /// <summary>
        /// Fecha final del rango de búsqueda.
        /// </summary>
        public DateOnly? CurrentEndDate { get; set; }

        /// <summary>
        /// Identificador del estado aplicado como filtro.
        /// </summary>
        public int? CurrentStatus { get; set; }

        /// <summary>
        /// Lista de clientes disponibles para filtrar.
        /// </summary>
        public List<SelectListItem>? AvailableClients { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// Lista de sucursales disponibles para filtrar.
        /// </summary>
        public List<SelectListItem>? AvailableBranches { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// Lista de conceptos disponibles para filtrar.
        /// </summary>
        public List<SelectListItem>? AvailableConcepts { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// Lista de estados disponibles para filtrar.
        /// </summary>
        public List<SelectListItem>? AvailableStatuses { get; set; } = new List<SelectListItem>();
    }
}