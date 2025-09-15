using System.ComponentModel.DataAnnotations;
using VCashApp.Enums;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    /// <summary>
    /// ViewModel para la visualización y procesamiento de la revisión final de una transacción de Centro de Efectivo.
    /// </summary>
    public class CefTransactionReviewViewModel
    {
        /// <summary>
        /// Identificador de la transacción.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Número de orden de servicio asociado.
        /// </summary>
        [Display(Name = "Orden de Servicio")]
        public string ServiceOrderId { get; set; } = string.Empty;

        /// <summary>
        /// Número de planilla de la transacción.
        /// </summary>
        [Display(Name = "Número de Planilla")]
        public int SlipNumber { get; set; }

        /// <summary>
        /// Tipo de transacción realizada.
        /// </summary>
        [Display(Name = "Tipo de Transacción")]
        public CefTransactionTypeEnum TransactionType { get; set; }

        /// <summary>
        /// Codigo de la transacción
        /// </summary>
        [Display(Name = "Codigo de Transacción")]
        public string? TransactionTypeCode { get; set; }

        /// <summary>
        /// Nombre de la transacción
        /// </summary>
        [Display(Name = "Nombre de la Transacción")]
        public string? TransactionTypeName { get; set; }

        /// <summary>
        /// Divisa en la que se realiza la transacción.
        /// </summary>
        [Display(Name = "Divisa")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Valor total declarado en la transacción.
        /// </summary>
        [Display(Name = "Monto Total Declarado")]
        public decimal TotalDeclaredValue { get; set; }

        /// <summary>
        /// Valor total contado en la transacción.
        /// </summary>
        [Display(Name = "Monto Total Contado")]
        public decimal TotalCountedValue { get; set; }

        /// <summary>
        /// Diferencia entre los valores declarado y contado.
        /// </summary>
        [Display(Name = "Diferencia de Valor")]
        public decimal ValueDifference { get; set; }

        /// <summary>
        /// Estado actual de la transacción.
        /// </summary>
        [Display(Name = "Estado Actual")]
        public CefTransactionStatusEnum CurrentStatus { get; set; }

        /// <summary>
        /// Usuario que realiza la revisión.
        /// </summary>
        [Display(Name = "Usuario de Revisión")]
        public string ReviewerUserName { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora en que se realizó la revisión.
        /// </summary>
        [Display(Name = "Fecha de Revisión")]
        public DateTime ReviewDate { get; set; }

        /// <summary>
        /// Resumen de los contenedores asociados.
        /// </summary>
        public List<CefContainerSummaryViewModel> ContainerSummaries { get; set; } = new List<CefContainerSummaryViewModel>();

        /// <summary>
        /// Resumen de las novedades registradas.
        /// </summary>
        public List<CefIncidentSummaryViewModel> IncidentSummaries { get; set; } = new List<CefIncidentSummaryViewModel>();

        /// <summary>
        /// Observaciones finales de la revisión.
        /// </summary>
        [Display(Name = "Observaciones Finales")]
        [StringLength(255, ErrorMessage = "Las observaciones no pueden exceder los 255 caracteres.")]
        public string? FinalObservations { get; set; }

        /// <summary>
        /// Estado final elegido tras la revisión.
        /// </summary>
        [Display(Name = "Nuevo Estado")]
        [Required(ErrorMessage = "Debe seleccionar un estado final para la revisión.")]
        public CefTransactionStatusEnum NewStatus { get; set; }

        /// <summary>
        /// Listado de estados disponibles para selección.
        /// </summary>
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? AvailableStatuses { get; set; }
    }

    /// <summary>
    /// ViewModel para el resumen de un contenedor en la vista de revisión.
    /// </summary>
    public class CefContainerSummaryViewModel
    {
        /// <summary>
        /// Identificador del contenedor.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Tipo de contenedor.
        /// </summary>
        [Display(Name = "Tipo")]
        public CefContainerTypeEnum ContainerType { get; set; }
        /// <summary>
        /// Código asignado al contenedor.
        /// </summary>
        [Display(Name = "Código")]
        public string ContainerCode { get; set; } = string.Empty;
        /// <summary>
        /// Valor declarado en el contenedor.
        /// </summary>
        [Display(Name = "Valor Declarado")]
        public decimal? DeclaredValue { get; set; }
        /// <summary>
        /// Valor contado en el contenedor.
        /// </summary>
        [Display(Name = "Valor Contado")]
        public decimal CountedValue { get; set; }
        /// <summary>
        /// Estado actual del contenedor.
        /// </summary>
        [Display(Name = "Estado")]
        public CefContainerStatusEnum ContainerStatus { get; set; }
        /// <summary>
        /// Usuario que procesó el contenedor.
        /// </summary>
        [Display(Name = "Usuario Proceso")]
        public string? ProcessingUserName { get; set; }
        /// <summary>
        /// Cantidad de novedades registradas.
        /// </summary>
        [Display(Name = "Novedades")]
        public int IncidentCount { get; set; }

        /// <summary>
        /// Detalles de los valores declarados y contados.
        /// </summary>
        public List<CefValueDetailSummaryViewModel> ValueDetailSummaries { get; set; } = new List<CefValueDetailSummaryViewModel>();
        /// <summary>
        /// Lista de novedades asociadas al contenedor.
        /// </summary>
        public List<CefIncidentSummaryViewModel> IncidentList { get; set; } = new List<CefIncidentSummaryViewModel>();
        /// <summary>
        /// Contenedores hijos en caso de contenedores anidados.
        /// </summary>
        public List<CefContainerSummaryViewModel> ChildContainers { get; set; } = new List<CefContainerSummaryViewModel>();
    }

    /// <summary>
    /// ViewModel para el resumen de un detalle de valor en la vista de revisión.
    /// </summary>
    public class CefValueDetailSummaryViewModel
    {
        /// <summary>
        /// Identificador del detalle de valor.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Tipo de valor registrado.
        /// </summary>
        [Display(Name = "Tipo")]
        public CefValueTypeEnum ValueType { get; set; }
        /// <summary>
        /// Descripción del detalle.
        /// </summary>
        [Display(Name = "Detalle")]
        public string DetailDescription { get; set; } = string.Empty;
        /// <summary>
        /// Monto calculado para el detalle.
        /// </summary>
        [Display(Name = "Monto")]
        public decimal CalculatedAmount { get; set; }
        /// <summary>
        /// Número de novedades registradas.
        /// </summary>
        [Display(Name = "Novedades")]
        public int IncidentCount { get; set; }
    }

    /// <summary>
    /// ViewModel para el resumen de una novedad en la vista de revisión.
    /// </summary>
    public class CefIncidentSummaryViewModel
    {
        /// <summary>
        /// Identificador de la novedad.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Tipo de novedad reportada.
        /// </summary>
        [Display(Name = "Tipo")]
        public CefIncidentTypeCategoryEnum IncidentType { get; set; }
        /// <summary>
        /// Descripción de la novedad.
        /// </summary>
        [Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Monto afectado por la novedad.
        /// </summary>
        [Display(Name = "Monto Afectado")]
        public decimal AffectedAmount { get; set; }
        /// <summary>
        /// Usuario que reportó la novedad.
        /// </summary>
        [Display(Name = "Reportado Por")]
        public string ReportingUserName { get; set; } = string.Empty;
    }
}