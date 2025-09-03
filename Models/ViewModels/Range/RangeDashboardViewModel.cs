using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels.Range
{
    /// <summary>
    /// ViewModel para la vista de dashboard de rangos.
    /// Contiene la lista paginada de rangos y los filtros aplicados.
    /// </summary>
    public class RangeDashboardViewModel
    {
        /// <summary>
        /// Lista de rangos resumidos que se muestran en la tabla.
        /// </summary>
        public IEnumerable<RangeSummaryViewModel> Ranges { get; set; } = new List<RangeSummaryViewModel>();

        /// <summary>
        /// Número de página actual.
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
        /// Total de registros encontrados según los filtros.
        /// </summary>
        public int TotalData { get; set; }

        /// <summary>
        /// Término de búsqueda ingresado por el usuario.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Cliente actualmente seleccionado como filtro.
        /// </summary>
        public int? CurrentClientId { get; set; }

        /// <summary>
        /// Estado actual del filtro (true = activo, false = inactivo, null = todos).
        /// </summary>
        public bool? RangeStatus { get; set; }

        /// <summary>
        /// Lista de clientes disponibles para el filtro en la vista.
        /// </summary>
        public List<SelectListItem>? AvailableClients { get; set; } = new();
    }
}