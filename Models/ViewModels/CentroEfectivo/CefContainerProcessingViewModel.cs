using System.ComponentModel.DataAnnotations;
using VCashApp.Enums;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    /// <summary>
    /// ViewModel para la entrada de datos durante el conteo de un contenedor de efectivo.
    /// </summary>
    public class CefContainerProcessingViewModel
    {
        public int Id { get; set; }

        [Display(Name = "ID de Transacción CEF")]
        [Required(ErrorMessage = "El ID de la Transacción CEF es requerido.")]
        public int CefTransactionId { get; set; }

        [Display(Name = "Contenedor Padre (ID)")]
        [Range(0, int.MaxValue, ErrorMessage = "El ID del contenedor padre debe ser un número válido.")]
        public int? ParentContainerId { get; set; }

        [Display(Name = "Tipo de Contenedor")]
        [Required(ErrorMessage = "El tipo de contenedor es requerido.")]
        public CefContainerTypeEnum ContainerType { get; set; }

        [Display(Name = "Código del Contenedor")]
        [Required(ErrorMessage = "El código del contenedor es requerido.")]
        [StringLength(100, ErrorMessage = "El código del contenedor no puede exceder los 100 caracteres.")]
        public string ContainerCode { get; set; } = string.Empty;

        [Display(Name = "Valor Declarado del Contenedor")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor válido.")]
        public decimal? DeclaredValue { get; set; }

        [Display(Name = "Estado del Contenedor")]
        public CefContainerStatusEnum ContainerStatus { get; set; }

        [Display(Name = "Observaciones del Contenedor")]
        [StringLength(255, ErrorMessage = "Las observaciones no pueden exceder los 255 caracteres.")]
        public string? Observations { get; set; }

        [Display(Name = "Cajero del Cliente (ID)")]
        public int? ClientCashierId { get; set; }
        [Display(Name = "Cajero del Cliente (Nombre)")]
        [StringLength(255, ErrorMessage = "El nombre del cajero del cliente no puede exceder los 255 caracteres.")]
        public string? ClientCashierName { get; set; }
        [Display(Name = "Fecha del Sobre/Bolsa del Cliente")]
        public DateOnly? ClientEnvelopeDate { get; set; }

        [Display(Name = "Valor Contado Actual")]
        public decimal CurrentCountedValue { get; set; }

        public string SobreMode { get; set; } = "Docs";

        public List<CefEnvelopeViewModel> Envelopes { get; set; } = new();
        public List<CefValueDetailViewModel> ValueDetails { get; set; } = new List<CefValueDetailViewModel>();
        public List<CefIncidentViewModel> Incidents { get; set; } = new List<CefIncidentViewModel>();
    }

    public class CefEnvelopeViewModel
    {
        public string? EnvelopeCode { get; set; }     // código del sobre (difiere del de la bolsa)
        public CefEnvelopeSubTypeEnum SubType { get; set; } = CefEnvelopeSubTypeEnum.Efectivo;
        public List<CefValueDetailViewModel> ValueDetails { get; set; } = new();
        public string? Observations { get; set; }
    }

    /// <summary>
    /// ViewModel para la entrada de datos de un detalle de valor (billete, moneda, cheque, documento) dentro de un contenedor.
    /// </summary>
    public class CefValueDetailViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Tipo de Valor")]
        [Required(ErrorMessage = "El tipo de valor es requerido.")]
        public CefValueTypeEnum ValueType { get; set; }

        [Display(Name = "Denominación")]
        [Required(ErrorMessage = "La denominación es requerida.")]
        public int Denomination { get; set; }

        [Display(Name = "Cantidad")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser una cantidad válida.")]
        public int? Quantity { get; set; }

        [Display(Name = "Cantidad de Fajos")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int? BundlesCount { get; set; }

        [Display(Name = "Cantidad de Picos")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int? LoosePiecesCount { get; set; }

        [Display(Name = "Valor Unitario")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor positivo.")]
        public decimal? UnitValue { get; set; }

        [Display(Name = "Monto Calculado")]
        public decimal CalculatedAmount { get; set; }

        [Display(Name = "¿Es de Alta Denominación?")]
        public bool? IsHighDenomination { get; set; } 

        [Display(Name = "Número de Identificador")]
        [StringLength(100, ErrorMessage = "El número identificador no puede exceder los 100 caracteres.")]
        public string? IdentifierNumber { get; set; }

        [Display(Name = "Banco")]
        [StringLength(100, ErrorMessage = "El nombre del banco no puede exceder los 100 caracteres.")]
        public string? BankName { get; set; }

        [Display(Name = "Fecha de Emisión")]
        [DataType(DataType.Date)]
        public DateOnly? IssueDate { get; set; }

        [Display(Name = "Emisor")]
        [StringLength(255, ErrorMessage = "El nombre del emisor no puede exceder los 255 caracteres.")]
        public string? Issuer { get; set; }

        [Display(Name = "Observaciones")]
        [StringLength(255, ErrorMessage = "Las observaciones no pueden exceder los 255 caracteres.")]
        public string? Observations { get; set; }
    }

    /// <summary>
    /// ViewModel para la entrada de datos de una novedad asociada a un contenedor o detalle de valor.
    /// </summary>
    public class CefIncidentViewModel
    {
        public int Id { get; set; }

        public int? CefTransactionId { get; set; }
        public int? CefContainerId { get; set; }
        public int? CefValueDetailId { get; set; }

        [Display(Name = "Tipo de Novedad")]
        [Required(ErrorMessage = "El tipo de novedad es requerido.")]
        public CefIncidentTypeCategoryEnum IncidentType { get; set; }

        [Display(Name = "Monto Afectado")]
        [Required(ErrorMessage = "El monto afectado es requerido.")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor positivo.")]
        public decimal AffectedAmount { get; set; }

        [Display(Name = "Denominación Afectada")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor válido.")]
        public decimal? AffectedDenomination { get; set; }

        [Display(Name = "Cantidad Afectada")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser una cantidad válida.")]
        public int? AffectedQuantity { get; set; }

        [Display(Name = "Descripción de la Novedad")]
        [Required(ErrorMessage = "La descripción de la novedad es requerida.")]
        [StringLength(255, ErrorMessage = "La descripción no puede exceder los 255 caracteres.")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Fecha y Hora de Reporte")]
        public DateTime ReportDate { get; set; } = DateTime.Now;

        [Display(Name = "Usuario que Reporta")]
        public string ReportingUserName { get; set; } = string.Empty;

        [Display(Name = "Estado de la Novedad")]
        public string IncidentStatus { get; set; } = "Reported";

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? IncidentTypes { get; set; }
    }
}