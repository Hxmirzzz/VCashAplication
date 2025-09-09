using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using VCashApp.Enums;
using VCashApp.Models.Entities;

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

        [Display(Name = "Código del Contenedor Padre")]
        public string? ParentContainerCode { get; set; }

        public int? BagViewIndex { get; set; }

        [Display(Name = "Tipo de Contenedor")]
        [Required(ErrorMessage = "El tipo de contenedor es requerido.")]
        public CefContainerTypeEnum ContainerType { get; set; }

        [Display(Name = "Código del Contenedor")]
        [Required(ErrorMessage = "El código del contenedor es requerido.")]
        [StringLength(100, ErrorMessage = "El código del contenedor no puede exceder los 100 caracteres.")]
        public string ContainerCode { get; set; } = string.Empty;

        [Display(Name = "Subtipo de Contenedor")]
        public CefEnvelopeSubTypeEnum? EnvelopeSubType { get; set; }

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
    public class CefValueDetailViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Display(Name = "Tipo de Valor")]
        [Required(ErrorMessage = "El tipo de valor es requerido.")]
        public CefValueTypeEnum ValueType { get; set; }

        [Display(Name = "Denominación")]
        public int? DenominationId { get; set; }

        [Display(Name = "Calidad")]
        public int? QualityId { get; set; }

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
        public decimal? UnitValue { get; set; }

        [Display(Name = "Monto Calculado")]
        public decimal CalculatedAmount { get; set; }

        [Display(Name = "¿Es de Alta Denominación?")]
        public bool IsHighDenomination { get; set; }

        [Display(Name = "Entidad Bancaria")]
        public string? EntitieBankId { get; set; }

        [Display(Name = "Número de Cuenta")]
        public int? AccountNumber { get; set; }

        [Display(Name = "Código del Cheque")]
        public int? CheckNumber { get; set; }

        [Display(Name = "Fecha de Emisión")]
        [DataType(DataType.Date)]
        public DateOnly? IssueDate { get; set; }

        [Display(Name = "Observaciones")]
        [StringLength(255, ErrorMessage = "Las observaciones no pueden exceder los 255 caracteres.")]
        public string? Observations { get; set; }

        public List<SelectListItem>? BankEntities { get; set; } = new List<SelectListItem>();
        public List<SelectListItem>? Denominations { get; set; }
        public List<SelectListItem>? Qualities { get; set; }
        public List<SelectListItem>? ValueTypes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Billete/Moneda: piden Denominación + Calidad
            if (ValueType == CefValueTypeEnum.Billete || ValueType == CefValueTypeEnum.Moneda)
            {
                if (DenominationId == null)
                    yield return new ValidationResult("La denominación es requerida.", new[] { nameof(DenominationId) });
                if (QualityId == null)
                    yield return new ValidationResult("La calidad es requerida.", new[] { nameof(QualityId) });
                if (UnitValue == null || UnitValue <= 0)
                    yield return new ValidationResult("El valor unitario es requerido.", new[] { nameof(UnitValue) });
            }

            // Documento/Cheque: piden Valor Unitario y Cantidad; NO piden denominación/calidad
            if (ValueType == CefValueTypeEnum.Documento)
            {
                if (Quantity <= 0)
                    yield return new ValidationResult("La cantidad es requerida.", new[] { nameof(Quantity) });
            }

            if (ValueType == CefValueTypeEnum.Cheque)
            {
                if (UnitValue == null || UnitValue <= 0)
                    yield return new ValidationResult("El valor unitario es requerido.", new[] { nameof(UnitValue) });
                if (Quantity <= 0)
                    yield return new ValidationResult("La cantidad es requerida.", new[] { nameof(Quantity) });
            }
        }
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

        public List<SelectListItem>? IncidentTypes { get; set; }
    }
}