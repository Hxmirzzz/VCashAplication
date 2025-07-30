using System.ComponentModel.DataAnnotations;
using VCashApp.Enums;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    /// <summary>
    /// ViewModel para la visualización y procesamiento de la revisión final de una transacción de Centro de Efectivo.
    /// </summary>
    public class CefTransactionReviewViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Orden de Servicio")]
        public string ServiceOrderId { get; set; } = string.Empty;

        [Display(Name = "Número de Planilla")]
        public int SlipNumber { get; set; }

        [Display(Name = "Tipo de Transacción")]
        public CefTransactionTypeEnum TransactionType { get; set; }

        [Display(Name = "Divisa")]
        public string Currency { get; set; } = string.Empty;

        [Display(Name = "Monto Total Declarado")]
        public decimal TotalDeclaredValue { get; set; }

        [Display(Name = "Monto Total Contado")]
        public decimal TotalCountedValue { get; set; }

        [Display(Name = "Diferencia de Valor")]
        public decimal ValueDifference { get; set; }

        [Display(Name = "Estado Actual")]
        public CefTransactionStatusEnum CurrentStatus { get; set; }

        [Display(Name = "Usuario de Revisión")]
        public string ReviewerUserName { get; set; } = string.Empty;

        [Display(Name = "Fecha de Revisión")]
        public DateTime ReviewDate { get; set; } 

        public List<CefContainerSummaryViewModel> ContainerSummaries { get; set; } = new List<CefContainerSummaryViewModel>();

        public List<CefIncidentSummaryViewModel> IncidentSummaries { get; set; } = new List<CefIncidentSummaryViewModel>();

        [Display(Name = "Observaciones Finales")]
        [StringLength(255, ErrorMessage = "Las observaciones no pueden exceder los 255 caracteres.")]
        public string? FinalObservations { get; set; }

        [Display(Name = "Nuevo Estado")]
        [Required(ErrorMessage = "Debe seleccionar un estado final para la revisión.")]
        public CefTransactionStatusEnum NewStatus { get; set; } // Using the English Enum for selection
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? AvailableStatuses { get; set; }
    }

    /// <summary>
    /// ViewModel para el resumen de un contenedor en la vista de revisión.
    /// </summary>
    public class CefContainerSummaryViewModel
    {
        public int Id { get; set; }
        [Display(Name = "Tipo")]
        public CefContainerTypeEnum ContainerType { get; set; } 
        [Display(Name = "Código")]
        public string ContainerCode { get; set; } = string.Empty;
        [Display(Name = "Valor Declarado")]
        public decimal? DeclaredValue { get; set; }
        [Display(Name = "Valor Contado")]
        public decimal CountedValue { get; set; }
        [Display(Name = "Estado")]
        public CefContainerStatusEnum ContainerStatus { get; set; }
        [Display(Name = "Usuario Proceso")]
        public string? ProcessingUserName { get; set; }
        [Display(Name = "Novedades")]
        public int IncidentCount { get; set; }

        public List<CefValueDetailSummaryViewModel> ValueDetailSummaries { get; set; } = new List<CefValueDetailSummaryViewModel>();
        public List<CefIncidentSummaryViewModel> IncidentList { get; set; } = new List<CefIncidentSummaryViewModel>();
        public List<CefContainerSummaryViewModel> ChildContainers { get; set; } = new List<CefContainerSummaryViewModel>(); // For nested envelopes
    }

    /// <summary>
    /// ViewModel para el resumen de un detalle de valor en la vista de revisión.
    /// </summary>
    public class CefValueDetailSummaryViewModel
    {
        public int Id { get; set; }
        [Display(Name = "Tipo")]
        public CefValueTypeEnum ValueType { get; set; }
        [Display(Name = "Detalle")]
        public string DetailDescription { get; set; } = string.Empty;
        [Display(Name = "Monto")]
        public decimal CalculatedAmount { get; set; }
        [Display(Name = "Novedades")]
        public int IncidentCount { get; set; }
    }

    /// <summary>
    /// ViewModel para el resumen de una novedad en la vista de revisión.
    /// </summary>
    public class CefIncidentSummaryViewModel
    {
        public int Id { get; set; }
        [Display(Name = "Tipo")]
        public CefIncidentTypeCategoryEnum IncidentType { get; set; } // Using the English Enum
        [Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;
        [Display(Name = "Monto Afectado")]
        public decimal AffectedAmount { get; set; }
        [Display(Name = "Reportado Por")]
        public string ReportingUserName { get; set; } = string.Empty;
    }
}