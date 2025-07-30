using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VCashApp.Enums;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    /// <summary>
    /// ViewModel for filters and pagination of the Cash Center dashboard.
    /// ViewModel para los filtros y paginación del dashboard de Centro de Efectivo.
    /// </summary>
    public class CefDashboardViewModel
    {
        public IEnumerable<CefTransactionSummaryViewModel> Transactions { get; set; } = new List<CefTransactionSummaryViewModel>();

        // Pagination and filter properties
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalData { get; set; }
        public string? SearchTerm { get; set; }
        public int? CurrentBranchId { get; set; }
        public DateOnly? CurrentStartDate { get; set; }
        public DateOnly? CurrentEndDate { get; set; }

        [Display(Name = "Estado Actual")]
        public CefTransactionStatusEnum? CurrentStatus { get; set; }

        // Properties for SelectLists (dropdowns)
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? AvailableBranches { get; set; }
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? TransactionStatuses { get; set; }
    }
}