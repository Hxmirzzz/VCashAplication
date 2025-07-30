using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    /// <summary>
    /// ViewModel para la visualización resumida de una transacción de Centro de Efectivo en listados.
    /// </summary>
    public class CefTransactionSummaryViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Orden de Servicio")]
        public string ServiceOrderId { get; set; } = string.Empty;

        [Display(Name = "Número de Planilla")]
        public int SlipNumber { get; set; }

        [Display(Name = "Divisa")]
        public string Currency { get; set; } = string.Empty;

        [Display(Name = "Tipo de Transacción")]
        public string TransactionType { get; set; } = string.Empty; // Value of enum as string for display

        [Display(Name = "Valor Declarado")]
        public decimal TotalDeclaredValue { get; set; }

        [Display(Name = "Valor Contado")]
        public decimal TotalCountedValue { get; set; }

        [Display(Name = "Diferencia")]
        public decimal ValueDifference { get; set; }

        [Display(Name = "Estado")]
        public string TransactionStatus { get; set; } = string.Empty; // Value of enum as string for display

        [Display(Name = "Fecha Registro")]
        public DateTime RegistrationDate { get; set; }

        // Additional properties for display in the summary
        [Display(Name = "Sucursal")]
        public string BranchName { get; set; } = string.Empty; // Obtained via join in the service

        [Display(Name = "Jefe de Turno")]
        public string HeadOfShiftName { get; set; } = string.Empty; // Obtained via join in the service
    }
}