using System.ComponentModel.DataAnnotations;
using VCashApp.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels.CentroEfectivo.Shared
{
    /// <summary>
    /// ViewModel para los filtros y paginación del dashboard de Centro de Efectivo.
    /// </summary>
    public class CefDashboardViewModel
    {
        /// <summary>
        /// Colección de transacciones a mostrar.
        /// </summary>
        public IEnumerable<CefTransactionSummaryViewModel> Transactions { get; set; } = new List<CefTransactionSummaryViewModel>();
        
        /// <summary>
        /// Tipo de dashborad a mostrar.
        /// </summary>
        public CefDashboardMode Mode { get; set; }

        /// <summary>
        /// Página actual de la paginación.
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
        /// Total de registros filtrados.
        /// </summary>
        public int TotalData { get; set; }
        /// <summary>
        /// Término de búsqueda actual.
        /// </summary>
        public string? SearchTerm { get; set; }
        /// <summary>
        /// Identificador de sucursal utilizado como filtro.
        /// </summary>
        public int? CurrentBranchId { get; set; }
        /// <summary>
        /// Fecha inicial del rango de búsqueda.
        /// </summary>
        public DateOnly? CurrentStartDate { get; set; }
        /// <summary>
        /// Fecha final del rango de búsqueda.
        /// </summary>
        public DateOnly? CurrentEndDate { get; set; }

        /// <summary>
        /// Estado actual seleccionado como filtro.
        /// </summary>
        [Display(Name = "Estado Actual")]
        public CefTransactionStatusEnum? CurrentStatus { get; set; }

        /// <summary>
        /// Lista de sucursales disponibles para filtrar.
        /// </summary>
        public List<SelectListItem>? AvailableBranches { get; set; }

        /// <summary>
        /// Lista de estados de transacción disponibles para filtrar.
        /// </summary>
        public List<SelectListItem>? TransactionStatuses { get; set; }
    }
}