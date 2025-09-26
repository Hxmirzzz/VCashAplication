using System.ComponentModel.DataAnnotations;
using VCashApp.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    /// <summary>
    /// ViewModel temporal para la creación unificada de un Servicio y su Transacción CEF inicial.
    /// Combina campos necesarios de AdmServicio y CefTransaction.
    /// </summary>
    public class CefServiceCreationViewModel
    {
        [Display(Name = "Numero de Planilla")]
        [Required(ErrorMessage = "El número de planilla es requerido.")]
        [Range(1, int.MaxValue, ErrorMessage = "El número de planilla debe ser un número positivo.")]
        public int SlipNumber { get; set; }

        [Display(Name = "Divisa")]
        [Required(ErrorMessage = "La divisa es requerida.")]
        [StringLength(3, ErrorMessage = "La divisa debe tener 3 caracteres.")]
        public string Currency { get; set; } = "COP";

        [Display(Name = "Tipo de Servicio")]
        [Required(ErrorMessage = "El tipo de servicio es requerido.")]
        public string ServiceConceptCode { get; set; } = string.Empty;

        [Display(Name = "Número de Pedido")]
        [StringLength(255, ErrorMessage = "El número de pedido no puede exceder los 255 caracteres.")]
        public string? ClientOrderNumber { get; set; }

        [Display(Name = "Código OS Cliente")]
        [StringLength(255, ErrorMessage = "El código OS Cliente no puede exceder los 255 caracteres.")]
        public string? ClientServiceOrderCode { get; set; }

        [Display(Name = "Sucursal Principal")]
        [Required(ErrorMessage = "La sucursal principal es requerida.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una Sucursal válida.")]
        public int BranchId { get; set; }
        public string? BranchName { get; set; }

        [Display(Name = "Fecha Solicitud")]
        [Required(ErrorMessage = "La fecha de solicitud es requerida.")]
        [DataType(DataType.Date)]
        public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Display(Name = "Hora Solicitud")]
        [Required(ErrorMessage = "La hora de solicitud es requerida.")]
        [DataType(DataType.Time)]
        public TimeOnly RequestTime { get; set; } = TimeOnly.FromDateTime(DateTime.Now);

        [Display(Name = "Fecha Programación")]
        [Required(ErrorMessage = "La fecha de programación es requerida.")]
        [DataType(DataType.Date)]
        public DateOnly ProgrammingDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

        [Display(Name = "Hora Programación")]
        [DataType(DataType.Time)]
        public TimeOnly? ProgrammingTime { get; set; }

        [Display(Name = "Modalidad de Servicio")]
        [StringLength(1, ErrorMessage = "La modalidad de servicio no puede exceder 1 carácter.")]
        public string? ServiceModality { get; set; }

        [Display(Name = "Observaciones del Servicio")]
        [StringLength(255, ErrorMessage = "Las observaciones no pueden exceder los 255 caracteres.")]
        public string? ServiceObservations { get; set; }

        [Display(Name = "¿Fallido?")]
        public bool IsFailed { get; set; } = false;

        [Display(Name = "Responsable del Fallo")]
        public string? FailedResponsible { get; set; }

        [Display(Name = "Razón del Fallo")]
        [StringLength(450, ErrorMessage = "La razón no puede exceder los 450 caracteres.")]
        public string? FailedReason { get; set; }

       
       
       
       

       
        [Display(Name = "Tipo de Origen")]
        [Required(ErrorMessage = "El tipo de origen es requerido.")]
        public LocationTypeEnum OriginType { get; set; }

        [Display(Name = "Código Cliente Origen")]
        [Required(ErrorMessage = "El código de cliente origen es requerido.")]
        public int OriginClientId { get; set; }

        [Display(Name = "Código Origen")]
        [Required(ErrorMessage = "El código de origen es requerido.")]
        [StringLength(25, ErrorMessage = "El código de origen no puede exceder los 25 caracteres.")]
        public string OriginCode { get; set; } = string.Empty;

        [Display(Name = "Código Ciudad Origen")]
        public int? OriginCityId { get; set; }
        public string? OriginCityName { get; set; }

        [Display(Name = "Código Sucursal Origen")]
        public int? OriginBranchId { get; set; }

        [Display(Name = "Código Rango Origen")]
        [StringLength(50, ErrorMessage = "El código de rango origen no puede exceder los 50 caracteres.")]
        public string? OriginRangeCode { get; set; }
        public string? OriginRangeDetails { get; set; }

       
        [Display(Name = "Tipo de Destino")]
        [Required(ErrorMessage = "El tipo de destino es requerido.")]
        public LocationTypeEnum DestinationType { get; set; }

        [Display(Name = "Código Cliente Destino")]
        [Required(ErrorMessage = "El código de cliente destino es requerido.")]
        public int DestinationClientId { get; set; }
            
        [Display(Name = "Código Destino")]
        [Required(ErrorMessage = "El código de destino es requerido.")]
        [StringLength(255, ErrorMessage = "El código de destino no puede exceder los 255 caracteres.")]
        public string DestinationCode { get; set; } = string.Empty;

        [Display(Name = "Código Ciudad Destino")]
        public int? DestinationCityId { get; set; }
        public string? DestinationCityName { get; set; }

        [Display(Name = "Código Sucursal Destino")]
        public int? DestinationBranchId { get; set; }

        [Display(Name = "Código Rango Destino")]
        [StringLength(255, ErrorMessage = "El código de rango destino no puede exceder los 255 caracteres.")]
        public string? DestinationRangeCode { get; set; }
        public string? DestinationRangeDetails { get; set; }

       
       
       

        [Display(Name = "Valor del Servicio")]
        [Required(ErrorMessage = "El valor del servicio es requerido.")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor numérico válido.")]
        public decimal ServiceValue { get; set; }

        [Display(Name = "Cantidad de Kits de Cambio")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int? ExchangeKitCount { get; set; }

        [Display(Name = "Cantidad de Bolsas Declaradas")]
        [Required(ErrorMessage = "La cantidad de bolsas declaradas es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int DeclaredBagCount { get; set; }

        [Display(Name = "Cantidad de Sobres Declarados")]
        [Required(ErrorMessage = "La cantidad de sobres declarados es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int DeclaredEnvelopeCount { get; set; }

        [Display(Name = "Cantidad de Cheques Declarados")]
        [Required(ErrorMessage = "La cantidad de cheques declarados es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int DeclaredCheckCount { get; set; }

        [Display(Name = "Cantidad de Documentos Declarados")]
        [Required(ErrorMessage = "La cantidad de documentos declarados es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int DeclaredDocumentCount { get; set; }

       
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

        [Display(Name = "Novedad Informativa Planilla")]
        [StringLength(255, ErrorMessage = "La novedad informativa no puede exceder los 255 caracteres.")]
        public string? InformativeIncident { get; set; }

        [Display(Name = "¿Es Custodia?")]
        public bool IsCustody { get; set; } = false;

        [Display(Name = "¿Es Punto a Punto?")]
        public bool IsPointToPoint { get; set; } = false;

        [Display(Name = "¿Quién Entrega?")]
        public string? DeliveryResponsible { get; set; } = string.Empty;

        [Display(Name = "¿Quién Recibe?")]
        public string? ReceptionResponsible { get; set; } = string.Empty;

       
       
       

       
        public List<SelectListItem>? AvailableCurrencies { get; set; }
        public List<SelectListItem>? AvailableServiceConcepts { get; set; }
        public List<SelectListItem>? AvailableBranches { get; set; }
        public List<SelectListItem>? AvailableClients { get; set; }
        public List<SelectListItem>? AvailableOriginPoints { get; set; }
        public List<SelectListItem>? AvailableOriginFunds { get; set; }
        public List<SelectListItem>? AvailableDestinationPoints { get; set; }
        public List<SelectListItem>? AvailableDestinationFunds { get; set; }
        public List<SelectListItem>? AvailableCities { get; set; }
        public List<SelectListItem>? AvailableRanks { get; set; }
        public List<SelectListItem>? AvailableVehicles { get; set; }
        public List<SelectListItem>? AvailableEmployees { get; set; }
        public List<SelectListItem>? AvailableServiceModalities { get; set; }
        public List<SelectListItem>? AvailableFailedResponsibles { get; set; }


       
        [Display(Name = "Fecha de Registro")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Display(Name = "Usuario de Registro")]
        public string RegistrationUserName { get; set; } = string.Empty;

        [Display(Name = "IP de Registro")]
        public string IPAddress { get; set; } = string.Empty;

       
        public string? RouteId { get; set; }
        public string? HeadOfShiftName { get; set; }
        public string? VehicleCode { get; set; }
    }
}