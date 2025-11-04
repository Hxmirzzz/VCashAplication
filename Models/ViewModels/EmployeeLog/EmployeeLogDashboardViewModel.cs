using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using VCashApp.Models.DTOs;

namespace VCashApp.Models.ViewModels.EmployeeLog
{
    public class EmployeeLogDashboardViewModel
    {
        // Filtros y paginación (estado de UI)
        public string? SearchTerm { get; set; }
        public int? CargoId { get; set; }
        public string? UnitId { get; set; }
        public int? BranchId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? LogStatus { get; set; } // 0: abierto, 1: completo

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalPages { get; set; }
        public int TotalData { get; set; }

        // Datos para selects
        public SelectList? Cargos { get; set; }
        public SelectList? Unidades { get; set; }
        public SelectList? Sucursales { get; set; }
        public SelectList? LogStatusList { get; set; }

        // Grid
        public IEnumerable<EmployeeLogSummaryViewModel> Logs { get; set; }
            = new List<EmployeeLogSummaryViewModel>();

        // Permisos UI
        public bool CanCreateLog { get; set; }
        public bool CanEditLog { get; set; }
        public bool CanViewHistory { get; set; }
    }
}