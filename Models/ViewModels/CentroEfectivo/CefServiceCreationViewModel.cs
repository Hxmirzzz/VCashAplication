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

        [Display(Name = "Tipo de Servicio")]
        [Required(ErrorMessage = "El tipo de servicio es requerido.")]
        public string ServiceConceptCode { get; set; } = string.Empty;

        [Display(Name = "Número de Pedido Cliente")]
        [StringLength(255, ErrorMessage = "El número de pedido no puede exceder los 255 caracteres.")]
        public string? ClientOrderNumber { get; set; }

        [Display(Name = "Código OS Cliente")]
        [StringLength(255, ErrorMessage = "El código OS Cliente no puede exceder los 255 caracteres.")]
        public string? ClientServiceOrderCode { get; set; } // Maps to CgsService.ClientServiceOrderCode

        [Display(Name = "Sucursal Principal")] // The main service branch
        [Required(ErrorMessage = "La sucursal principal es requerida.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una Sucursal válida.")]
        public int BranchId { get; set; } // Maps to CgsService.BranchCode (CodSuc)
        public string? BranchName { get; set; } // For UI display (not in CgsService, but obtained from AdmSucursal)

        [Display(Name = "Fecha Solicitud")]
        [Required(ErrorMessage = "La fecha de solicitud es requerida.")]
        [DataType(DataType.Date)]
        public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.Now); // Maps to CgsService.RequestDate

        [Display(Name = "Hora Solicitud")]
        [Required(ErrorMessage = "La hora de solicitud es requerida.")]
        [DataType(DataType.Time)]
        public TimeOnly RequestTime { get; set; } = TimeOnly.FromDateTime(DateTime.Now); // Maps to CgsService.RequestTime

        [Display(Name = "Fecha Programación")]
        [Required(ErrorMessage = "La fecha de programación es requerida.")]
        [DataType(DataType.Date)]
        public DateOnly ProgrammingDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(1)); // Maps to CgsService.ProgrammingDate

        [Display(Name = "Hora Programación")]
        [DataType(DataType.Time)]
        public TimeOnly? ProgrammingTime { get; set; } // Maps to CgsService.ProgrammingTime

        [Display(Name = "Modalidad de Servicio")]
        [StringLength(1, ErrorMessage = "La modalidad de servicio no puede exceder 1 carácter.")]
        public string? ServiceModality { get; set; } // Maps to CgsService.ServiceModality

        [Display(Name = "Observaciones del Servicio")]
        [StringLength(255, ErrorMessage = "Las observaciones no pueden exceder los 255 caracteres.")]
        public string? ServiceObservations { get; set; } // Maps to CgsService.Observations

        // =============================================================
        // ORIGIN AND DESTINATION FIELDS (FOR AdmServicio - CgsService POCO)
        // These fields are input by the user as the full Services module is not ready.
        // =============================================================

        // Origin
        [Display(Name = "Tipo de Origen")]
        [Required(ErrorMessage = "El tipo de origen es requerido.")]
        public LocationTypeEnum OriginType { get; set; } // Enum: Point, Fund. Used to derive CgsService.OriginIndicatorType

        [Display(Name = "Código Cliente Origen")]
        [Required(ErrorMessage = "El código de cliente origen es requerido.")]
        public int OriginClientId { get; set; } // Maps to CgsService.OriginClientCode

        [Display(Name = "Nombre Cliente Origen")]
        [Required(ErrorMessage = "El nombre de cliente origen es requerido.")]
        [StringLength(255, ErrorMessage = "El nombre de cliente origen no puede exceder los 255 caracteres.")]
        public string OriginClientName { get; set; } = string.Empty; // Maps to CgsService.ClienteOrigen in SP (not in CgsService POCO)

        [Display(Name = "Código Origen")] // Code of the Point or Fund
        [Required(ErrorMessage = "El código de origen es requerido.")]
        [StringLength(25, ErrorMessage = "El código de origen no puede exceder los 25 caracteres.")]
        public string OriginCode { get; set; } = string.Empty; // Maps to CgsService.OriginPointCode

        [Display(Name = "Nombre Origen")] // Name of the Point or Fund
        [Required(ErrorMessage = "El nombre de origen es requerido.")]
        [StringLength(255, ErrorMessage = "El nombre de origen no puede exceder los 255 caracteres.")]
        public string OriginName { get; set; } = string.Empty; // Maps to CgsService.PuntoOrigen in SP (not in CgsService POCO)

        [Display(Name = "Código Ciudad Origen")]
        public int? OriginCityId { get; set; } // Maps to CodCiudadOrigen in SP (not in CgsService POCO)
        public string? OriginCityName { get; set; } // For UI display

        [Display(Name = "Código Sucursal Origen")]
        public int? OriginBranchId { get; set; } // Maps to CodSucursalOrigen in SP (not in CgsService POCO)

        [Display(Name = "Código Rango Origen")]
        [StringLength(50, ErrorMessage = "El código de rango origen no puede exceder los 50 caracteres.")]
        public string? OriginRangeCode { get; set; } // Maps to CodRangoOrigen in SP (not in CgsService POCO)
        public string? OriginRangeDetails { get; set; } // For UI display

        // Destination
        [Display(Name = "Tipo de Destino")]
        [Required(ErrorMessage = "El tipo de destino es requerido.")]
        public LocationTypeEnum DestinationType { get; set; } // Enum: Point, Fund. Used to derive CgsService.DestinationIndicatorType

        [Display(Name = "Código Cliente Destino")]
        [Required(ErrorMessage = "El código de cliente destino es requerido.")]
        public int DestinationClientId { get; set; } // Maps to CgsService.DestinationClientCode

        [Display(Name = "Nombre Cliente Destino")]
        [Required(ErrorMessage = "El nombre de cliente destino es requerido.")]
        [StringLength(255, ErrorMessage = "El nombre de cliente destino no puede exceder los 255 caracteres.")]
        public string DestinationClientName { get; set; } = string.Empty; // Maps to CgsService.ClienteDestino in SP (not in CgsService POCO)

        [Display(Name = "Código Destino")] // Code of the Point or Fund
        [Required(ErrorMessage = "El código de destino es requerido.")]
        [StringLength(255, ErrorMessage = "El código de destino no puede exceder los 255 caracteres.")]
        public string DestinationCode { get; set; } = string.Empty; // Maps to CgsService.DestinationPointCode

        [Display(Name = "Nombre Destino")] // Name of the Point or Fund
        [Required(ErrorMessage = "El nombre de destino es requerido.")]
        [StringLength(255, ErrorMessage = "El nombre de destino no puede exceder los 255 caracteres.")]
        public string DestinationName { get; set; } = string.Empty; // Maps to CgsService.PuntoDestino in SP (not in CgsService POCO)

        [Display(Name = "Código Ciudad Destino")]
        public int? DestinationCityId { get; set; } // Maps to CodCiudadDestino in SP (not in CgsService POCO)
        public string? DestinationCityName { get; set; } // For UI display

        [Display(Name = "Código Sucursal Destino")]
        public int? DestinationBranchId { get; set; } // Maps to CodSucursalDestino in SP (not in CgsService POCO)

        [Display(Name = "Código Rango Destino")]
        [StringLength(255, ErrorMessage = "El código de rango destino no puede exceder los 255 caracteres.")]
        public string? DestinationRangeCode { get; set; } // Maps to CodRangoDestino in SP (not in CgsService POCO)
        public string? DestinationRangeDetails { get; set; } // For UI display

        // =============================================================
        // VALUE AND QUANTITY FIELDS (FOR AdmServicio - CgsService POCO & CefTransaction POCO)
        // =============================================================

        [Display(Name = "Valor del Servicio")]
        [Required(ErrorMessage = "El valor del servicio es requerido.")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor numérico válido.")]
        public decimal ServiceValue { get; set; } // Maps to CgsService.ServiceValue

        [Display(Name = "Cantidad de Kits de Cambio")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int? ExchangeKitCount { get; set; } // Maps to CgsService.NumberOfChangeKits

        // Declared Quantities (initial for CefTransaction)
        [Display(Name = "Cantidad de Bolsas Declaradas")]
        [Required(ErrorMessage = "La cantidad de bolsas declaradas es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int DeclaredBagCount { get; set; } // Maps to CgsService.NumberOfCoinBags & CefTransaction.DeclaredBagCount

        [Display(Name = "Cantidad de Sobres Declarados")]
        [Required(ErrorMessage = "La cantidad de sobres declarados es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int DeclaredEnvelopeCount { get; set; } // Maps to CefTransaction.DeclaredEnvelopeCount

        [Display(Name = "Cantidad de Cheques Declarados")]
        [Required(ErrorMessage = "La cantidad de cheques declarados es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int DeclaredCheckCount { get; set; } // Maps to CefTransaction.DeclaredCheckCount

        [Display(Name = "Cantidad de Documentos Declarados")]
        [Required(ErrorMessage = "La cantidad de documentos declarados es requerida.")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe ser un número válido.")]
        public int DeclaredDocumentCount { get; set; } // Maps to CefTransaction.DeclaredDocumentCount

        // Declared Monetary Values
        [Display(Name = "Valor en Billetes Declarado")]
        [Required(ErrorMessage = "El valor en billetes declarado es requerido.")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor numérico válido.")]
        public decimal DeclaredBillValue { get; set; } // Maps to CgsService.BillValue & CefTransaction.DeclaredBillValue

        [Display(Name = "Valor en Monedas Declarado")]
        [Required(ErrorMessage = "El valor en monedas declarado es requerido.")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor numérico válido.")]
        public decimal DeclaredCoinValue { get; set; } // Maps to CgsService.CoinValue & CefTransaction.DeclaredCoinValue

        [Display(Name = "Valor de Documentos Declarado")]
        [Required(ErrorMessage = "El valor de documentos declarado es requerido.")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "Debe ser un valor numérico válido.")]
        public decimal DeclaredDocumentValue { get; set; } // Maps to CefTransaction.DeclaredDocumentValue

        [Display(Name = "Valor Total Declarado")]
        public decimal TotalDeclaredValue { get; set; } // Will be calculated

        [Display(Name = "Novedad Informativa Planilla")]
        [StringLength(255, ErrorMessage = "La novedad informativa no puede exceder los 255 caracteres.")]
        public string? InformativeIncident { get; set; } // Maps to CefTransaction.InformativeIncident

        [Display(Name = "¿Es Custodia?")]
        public bool IsCustody { get; set; } = false; // Maps to CefTransaction.IsCustody

        [Display(Name = "¿Es Punto a Punto?")]
        public bool IsPointToPoint { get; set; } = false; // Maps to CefTransaction.IsPointToPoint

        // =============================================================
        // COMMON CEF / SERVICE FIELDS (DISPLAY & SELECTION)
        // =============================================================

        // Properties for SelectLists (dropdowns in the frontend)
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
        public List<SelectListItem>? AvailableServiceModalities { get; set; } // You'll need to define how to populate this

        // Operator Data (Automatic, for display)
        [Display(Name = "Fecha de Registro")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Display(Name = "Usuario de Registro")]
        public string RegistrationUserName { get; set; } = string.Empty;

        [Display(Name = "IP de Registro")]
        public string IPAddress { get; set; } = string.Empty;

        // Route Data (for display if linked to a route, not filled here)
        public string? RouteId { get; set; }
        public string? HeadOfShiftName { get; set; }
        public string? VehicleCode { get; set; }
    }
}