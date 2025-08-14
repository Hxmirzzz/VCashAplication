using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    /// <summary>
    /// ViewModel para la visualización resumida de una transacción de Centro de Efectivo en listados.
    /// </summary>
    public class CefTransactionSummaryViewModel
    {
        /// <summary>
        /// Identificador de la transacción.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Número de orden de servicio.
        /// </summary>
        [Display(Name = "Orden de Servicio")]
        public string ServiceOrderId { get; set; } = string.Empty;

        /// <summary>
        /// Número de planilla de la transacción.
        /// </summary>
        [Display(Name = "Número de Planilla")]
        public int SlipNumber { get; set; }

        /// <summary>
        /// Divisa utilizada en la transacción.
        /// </summary>
        [Display(Name = "Divisa")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de transacción representado como texto.
        /// </summary>
        [Display(Name = "Tipo de Transacción")]
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Valor total declarado.
        /// </summary>
        [Display(Name = "Valor Declarado")]
        public decimal TotalDeclaredValue { get; set; }

        /// <summary>
        /// Valor total contado.
        /// </summary>
        [Display(Name = "Valor Contado")]
        public decimal TotalCountedValue { get; set; }

        /// <summary>
        /// Diferencia entre valores declarado y contado.
        /// </summary>
        [Display(Name = "Diferencia")]
        public decimal ValueDifference { get; set; }

        /// <summary>
        /// Estado de la transacción representado como texto.
        /// </summary>
        [Display(Name = "Estado")]
        public string TransactionStatus { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de registro de la transacción.
        /// </summary>
        [Display(Name = "Fecha Registro")]
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// Nombre de la sucursal donde se realizó la transacción.
        /// </summary>
        [Display(Name = "Sucursal")]
        public string BranchName { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del jefe de turno asignado.
        /// </summary>
        [Display(Name = "Jefe de Turno")]
        public string HeadOfShiftName { get; set; } = string.Empty;
    }
}