using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VCashApp.Enums; // Asegúrate de que este namespace apunte a tus enums

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    /// <summary>
    /// ViewModel para la entrada de datos durante el proceso de Check-in de una nueva Transacción de Centro de Efectivo.
    /// </summary>
    public class CefTransactionCheckinViewModel
    {
        [Display(Name = "Orden de Servicio")]
        [Required(ErrorMessage = "La Orden de Servicio es requerida.")]
        [StringLength(20, ErrorMessage = "La Orden de Servicio no puede exceder los 20 caracteres.")]
        public string ServiceOrderId { get; set; } = string.Empty;

        [Display(Name = "ID de Ruta Diaria")]
        [StringLength(12, ErrorMessage = "El ID de Ruta Diaria no puede exceder los 12 caracteres.")]
        public string? RouteId { get; set; }

        [Display(Name = "Numero de Planilla")]
        [Required(ErrorMessage = "El número de planilla es requerido.")]
        [Range(1, int.MaxValue, ErrorMessage = "El número de planilla debe ser mayor a 0.")]
        public int? SlipNumber { get; set; }

        [Display(Name = "Divisa")]
        [Required(ErrorMessage = "La divisa es requerida.")]
        [StringLength(3, ErrorMessage = "La divisa debe tener 3 caracteres.")]
        public string Currency { get; set; } = string.Empty;

        [Display(Name = "Tipo de Transacción")]
        [Required(ErrorMessage = "El tipo de transacción es requerido.")]
        public string? TransactionType { get; set; }

        // Declared Quantities (for bags, envelopes, checks, documents)
        [Display(Name = "Cantidad de Bolsas Declaradas")]
        [Required(ErrorMessage = "La cantidad de bolsas declaradas es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de bolsas debe ser un número válido.")]
        public int DeclaredBagCount { get; set; }

        [Display(Name = "Cantidad de Sobres Declarados")]
        [Required(ErrorMessage = "La cantidad de sobres declarados es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de sobres debe ser un número válido.")]
        public int DeclaredEnvelopeCount { get; set; }

        [Display(Name = "Cantidad de Cheques Declarados")]
        [Required(ErrorMessage = "La cantidad de cheques declarados es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de cheques debe ser un número válido.")]
        public int DeclaredCheckCount { get; set; }

        [Display(Name = "Cantidad de Documentos Declarados")]
        [Required(ErrorMessage = "La cantidad de documentos declarados es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de documentos debe ser un número válido.")]
        public int DeclaredDocumentCount { get; set; }

        // Declared Monetary Values
        [Display(Name = "Valor en Billetes Declarado")]
        [Required(ErrorMessage = "El valor en billetes declarado es requerido.")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor numérico válido.")]
        public decimal DeclaredBillValue { get; set; }

        [Display(Name = "Valor en Monedas Declarado")]
        [Required(ErrorMessage = "El valor en monedas declarado es requerido.")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor numérico válido.")]
        public decimal DeclaredCoinValue { get; set; }

        [Display(Name = "Valor de Documentos Declarado")]
        [Required(ErrorMessage = "El valor de documentos declarado es requerido.")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor numérico válido.")]
        public decimal DeclaredDocumentValue { get; set; }

        [Display(Name = "Valor Total Declarado")]
        public decimal TotalDeclaredValue { get; set; }

        // Indicators
        [Display(Name = "¿Es Custodia?")]
        public bool IsCustody { get; set; } = false;

        [Display(Name = "¿Es Punto a Punto?")]
        public bool IsPointToPoint { get; set; } = false;

        [Display(Name = "Novedad Informativa")]
        [StringLength(255, ErrorMessage = "La novedad informativa no puede exceder los 255 caracteres.")]
        public string? InformativeIncident { get; set; }

        // Operator Data (display only, populated by controller)
        [Display(Name = "Fecha de Registro")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Display(Name = "Usuario de Registro")]
        public string RegistrationUserName { get; set; } = string.Empty;

        [Display(Name = "IP de Registro")]
        public string IPAddress { get; set; } = string.Empty;

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? Currencies { get; set; }
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? TransactionTypes { get; set; }

        // Service/Route data to display in UI
        [Display(Name = "Cliente")]
        public string? ClientName { get; set; }
        [Display(Name = "Sucursal")]
        public string? BranchName { get; set; }
        [Display(Name = "Origen")]
        public string? OriginLocationName { get; set; }
        [Display(Name = "Destino")]
        public string? DestinationLocationName { get; set; }
        [Display(Name = "Jefe de Turno")]
        public string? HeadOfShiftName { get; set; }
        [Display(Name = "Vehículo")]
        public string? VehicleCode { get; set; }
    }
}