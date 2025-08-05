using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;

namespace VCashApp.Models.ViewModels.Servicio
{
    /// <summary>
    /// ViewModel para la visualización resumida de una solicitud de servicio en listados.
    /// </summary>
    public class CgsServiceSummaryViewModel
    {
        [Display(Name = "Orden de Servicio")]
        public string ServiceOrderId { get; set; } = string.Empty;

        [Display(Name = "Clave")]
        public int KeyValue { get; set; }

        [Display(Name = "Cliente")]
        public string ClientName { get; set; } = string.Empty;

        [Display(Name = "Sucursal")]
        public string BranchName { get; set; } = string.Empty;

        [Display(Name = "Origen")]
        public string OriginPointName { get; set; } = string.Empty;

        [Display(Name = "Destino")]
        public string DestinationPointName { get; set; } = string.Empty;

        [Display(Name = "Concepto")]
        public string ConceptName { get; set; } = string.Empty;

        [Display(Name = "Fecha Solicitud")]
        [DataType(DataType.Date)]
        public DateOnly RequestDate { get; set; }

        [Display(Name = "Hora Solicitud")]
        [DataType(DataType.Time)]
        public TimeOnly RequestTime { get; set; }

        [Display(Name = "Fecha Programación")]
        [DataType(DataType.Date)]
        public DateOnly? ProgrammingDate { get; set; }

        [Display(Name = "Hora Programación")]
        [DataType(DataType.Time)]
        public TimeOnly? ProgrammingTime { get; set; }

        [Display(Name = "Código de Estado")]
        public int StatusCode { get; set; }

        [Display(Name = "Estado")]
        public string StatusName { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel para el formulario de creación y edición de solicitudes de servicio en el Centro de Gestión de Servicios.
    /// </summary>
    public class CgsServiceRequestViewModel
    {
        public string? ServiceOrderId { get; set; }

        [Display(Name = "Número de Pedido")]
        [StringLength(255, ErrorMessage = "El número de pedido no puede exceder los 255 caracteres.")]
        public string? RequestNumber { get; set; }

        [Display(Name = "Código OS Cliente")]
        [StringLength(255, ErrorMessage = "El código OS del cliente no puede exceder los 255 caracteres.")]
        public string? ClientServiceOrderCode { get; set; }

        [Display(Name = "Cliente Principal")]
        [Required(ErrorMessage = "El cliente principal es requerido.")]
        public int ClientCode { get; set; }

        [Display(Name = "Sucursal Principal")]
        [Required(ErrorMessage = "La sucursal principal es requerida.")]
        public int BranchCode { get; set; }

        [Display(Name = "Fecha de Solicitud")]
        [Required(ErrorMessage = "La fecha de solicitud es requerida.")]
        [DataType(DataType.Date)]
        public DateOnly RequestDate { get; set; }

        [Display(Name = "Hora de Solicitud")]
        [Required(ErrorMessage = "La hora de solicitud es requerida.")]
        [DataType(DataType.Time)]
        public TimeOnly RequestTime { get; set; }

        [Display(Name = "Concepto de Servicio")]
        [Required(ErrorMessage = "El concepto de servicio es requerido.")]
        public int ConceptCode { get; set; }

        [Display(Name = "Tipo de Traslado")]
        [StringLength(1, ErrorMessage = "El tipo de traslado debe ser un carácter.")]
        public string? TransferType { get; set; }

        [Display(Name = "Estado")]
        [Required(ErrorMessage = "El estado es requerido.")]
        public int StatusCode { get; set; }

        [Display(Name = "Código de Flujo")]
        public int? FlowCode { get; set; }

        // ORIGIN FIELDS

        [Display(Name = "Cliente de Origen")]
        public int? OriginClientCode { get; set; }

        [Display(Name = "Nombre Cliente Origen")]
        [Required(ErrorMessage = "El nombre de cliente origen es requerido.")]
        [StringLength(255, ErrorMessage = "El nombre de cliente origen no puede exceder los 255 caracteres.")]
        public string OriginClientName { get; set; } = string.Empty;

        [Display(Name = "Tipo de Origen")]
        [Required(ErrorMessage = "El tipo de origen es requerido.")]
        [StringLength(1, ErrorMessage = "El tipo de origen debe ser un carácter ('P' para Punto, 'F' para Fondo).")]
        public string OriginIndicatorType { get; set; } = "P";

        [Display(Name = "Punto/Fondo de Origen")]
        [Required(ErrorMessage = "El punto o fondo de origen es requerido.")]
        [StringLength(25, ErrorMessage = "El código de origen no puede exceder los 25 caracteres.")]
        public string OriginPointCode { get; set; } = null!;

        [Display(Name = "Nombre Origen")]
        [Required(ErrorMessage = "El nombre de origen es requerido.")]
        [StringLength(255, ErrorMessage = "El nombre de origen no puede exceder los 255 caracteres.")]
        public string OriginName { get; set; } = string.Empty;

        [Display(Name = "Código Ciudad Origen")]
        public int? OriginCityId { get; set; }
        public string? OriginCityName { get; set; }

        [Display(Name = "Código Sucursal Origen")]
        public int? OriginBranchId { get; set; }

        [Display(Name = "Código Rango Origen")]
        [StringLength(50, ErrorMessage = "El código de rango origen no puede exceder los 50 caracteres.")]
        public string? OriginRangeCode { get; set; }
        public string? OriginRangeDetails { get; set; }

        // DESTINATION FIELDS

        [Display(Name = "Cliente de Destino")]
        public int? DestinationClientCode { get; set; }

        [Display(Name = "Nombre Cliente Destino")]
        [Required(ErrorMessage = "El nombre de cliente destino es requerido.")]
        [StringLength(255, ErrorMessage = "El nombre de cliente destino no puede exceder los 255 caracteres.")]
        public string DestinationClientName { get; set; } = string.Empty;

        [Display(Name = "Tipo de Destino")]
        [Required(ErrorMessage = "El tipo de destino es requerido.")]
        [StringLength(1, ErrorMessage = "El tipo de destino debe ser un carácter ('P' para Punto, 'F' para Fondo).")]
        public string DestinationIndicatorType { get; set; } = "F";

        [Display(Name = "Punto/Fondo de Destino")]
        [Required(ErrorMessage = "El punto o fondo de destino es requerido.")]
        [StringLength(255, ErrorMessage = "El código de destino no puede exceder los 255 caracteres.")]
        public string DestinationPointCode { get; set; } = null!;

        [Display(Name = "Nombre Destino")]
        [Required(ErrorMessage = "El nombre de destino es requerido.")]
        [StringLength(255, ErrorMessage = "El nombre de destino no puede exceder los 255 caracteres.")]
        public string DestinationName { get; set; } = string.Empty;

        [Display(Name = "Código Ciudad Destino")]
        public int? DestinationCityId { get; set; }
        public string? DestinationCityName { get; set; }

        [Display(Name = "Código Sucursal Destino")]
        public int? DestinationBranchId { get; set; }

        [Display(Name = "Código Rango Destino")]
        [StringLength(255, ErrorMessage = "El código de rango destino no puede exceder los 255 caracteres.")]
        public string? DestinationRangeCode { get; set; }
        public string? DestinationRangeDetails { get; set; }

        // DATES AND TIMES

        [Display(Name = "Fecha de Aceptación")]
        [DataType(DataType.Date)]
        public DateOnly? AcceptanceDate { get; set; }

        [Display(Name = "Hora de Aceptación")]
        [DataType(DataType.Time)]
        public TimeOnly? AcceptanceTime { get; set; }

        [Display(Name = "Fecha de Programación")]
        [DataType(DataType.Date)]
        public DateOnly? ProgrammingDate { get; set; }

        [Display(Name = "Hora de Programación")]
        [DataType(DataType.Time)]
        public TimeOnly? ProgrammingTime { get; set; }

        [Display(Name = "Fecha de Atención Inicial")]
        [DataType(DataType.Date)]
        public DateOnly? InitialAttentionDate { get; set; }

        [Display(Name = "Hora de Atención Inicial")]
        [DataType(DataType.Time)]
        public TimeOnly? InitialAttentionTime { get; set; }

        [Display(Name = "Fecha de Atención Final")]
        [DataType(DataType.Date)]
        public DateOnly? FinalAttentionDate { get; set; }

        [Display(Name = "Hora de Atención Final")]
        [DataType(DataType.Time)]
        public TimeOnly? FinalAttentionTime { get; set; }

        [Display(Name = "Fecha de Cancelación")]
        [DataType(DataType.Date)]
        public DateOnly? CancellationDate { get; set; }

        [Display(Name = "Hora de Cancelación")]
        [DataType(DataType.Time)]
        public TimeOnly? CancellationTime { get; set; }

        [Display(Name = "Personal de Cancelación")]
        [StringLength(255, ErrorMessage = "El nombre del personal de cancelación no puede exceder los 255 caracteres.")]
        public string? CancellationPerson { get; set; }

        [Display(Name = "Razón de Cancelación")]
        [StringLength(450, ErrorMessage = "La razón de cancelación no puede exceder los 450 caracteres.")]
        public string? CancellationReason { get; set; }

        [Display(Name = "Usuario de Cancelación")]
        public string? CancellationOperator { get; set; }

        [Display(Name = "Fecha de Rechazo")]
        [DataType(DataType.Date)]
        public DateOnly? RejectionDate { get; set; }

        [Display(Name = "Hora de Rechazo")]
        [DataType(DataType.Time)]
        public TimeOnly? RejectionTime { get; set; }

        [Display(Name = "¿Fallido?")]
        public bool IsFailed { get; set; }

        [Display(Name = "Responsable del Fallo")]
        public string? FailedResponsible { get; set; }

        [Display(Name = "Razón del Fallo")]
        [StringLength(450, ErrorMessage = "La razón no puede exceder los 450 caracteres.")]
        public string? FailedReason { get; set; }

        [Display(Name = "Valor en Billetes")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "El valor en billetes debe ser un número válido.")]
        public decimal? BillValue { get; set; } = 0;

        [Display(Name = "Valor en Monedas")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "El valor en monedas debe ser un número válido.")]
        public decimal? CoinValue { get; set; } = 0;

        [Display(Name = "Valor Total del Servicio")]
        [Range(0.00, (double)decimal.MaxValue, ErrorMessage = "El valor del servicio debe ser un número válido.")]
        public decimal? ServiceValue { get; set; } = 0;

        [Display(Name = "Número de Kits de Cambio")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de kits de cambio debe ser un número válido.")]
        public int? NumberOfChangeKits { get; set; } = 0;

        [Display(Name = "Número de Bolsas de Moneda")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de bolsas de moneda debe ser un número válido.")]
        public int? NumberOfCoinBags { get; set; } = 0;

        [Display(Name = "Observaciones")]
        [StringLength(255, ErrorMessage = "Las observaciones no pueden exceder los 255 caracteres.")]
        public string? Observations { get; set; }

        [Display(Name = "Modalidad de Servicio")]
        [StringLength(1, ErrorMessage = "La modalidad de servicio debe ser un carácter.")]
        public string? ServiceModality { get; set; }

        [Display(Name = "Clave")]
        public int? KeyValue { get; set; }

        [Display(Name = "Archivo Adjunto")]
        [StringLength(450, ErrorMessage = "El archivo adjunto no puede exceder los 255 caracteres.")]
        public string? DetailFile { get; set; }

        public List<SelectListItem>? AvailableClients { get; set; }
        public List<SelectListItem>? AvailableBranches { get; set; }
        public List<SelectListItem>? AvailableConcepts { get; set; }
        public List<SelectListItem>? AvailableStatuses { get; set; }
        public List<SelectListItem>? AvailableTransferTypes { get; set; }
        public List<SelectListItem>? AvailableServiceModalities { get; set; }

        public List<SelectListItem>? AvailableOriginPoints { get; set; }
        public List<SelectListItem>? AvailableOriginFunds { get; set; }
        public List<SelectListItem>? AvailableDestinationPoints { get; set; }
        public List<SelectListItem>? AvailableDestinationFunds { get; set; }
        public List<SelectListItem>? AvailableFailedResponsibles { get; set; }


        [Display(Name = "Usuario de Registro")]
        public string? CgsOperatorUserName { get; set; }

        [Display(Name = "Sucursal del Operador")]
        [StringLength(255, ErrorMessage = "El nombre de la sucursal del operador no puede exceder los 255 caracteres.")]
        public string? OperatorBranchName { get; set; }

        [Display(Name = "IP de Registro")]
        public string? OperatorIpAddress { get; set; }

        public string? CurrentStatusName { get; set; }
        public string? CurrentConceptName { get; set; }
        public string? CurrentBranchName { get; set; }
        public string? CurrentClientName { get; set; }
        public string? CurrentOriginClientName { get; set; }
        public string? CurrentDestinationClientName { get; set; }
        public string? CurrentOriginPointName { get; set; }
        public string? CurrentDestinationPointName { get; set; }

        public CgsServiceRequestViewModel()
        {
            AvailableClients = new List<SelectListItem>();
            AvailableBranches = new List<SelectListItem>();
            AvailableConcepts = new List<SelectListItem>();
            AvailableStatuses = new List<SelectListItem>();
            AvailableTransferTypes = new List<SelectListItem>();
            AvailableServiceModalities = new List<SelectListItem>();

            AvailableOriginPoints = new List<SelectListItem>();
            AvailableOriginFunds = new List<SelectListItem>();
            AvailableDestinationPoints = new List<SelectListItem>();
            AvailableDestinationFunds = new List<SelectListItem>();

            RequestDate = DateOnly.FromDateTime(DateTime.Now);
            RequestTime = TimeOnly.FromDateTime(DateTime.Now);
            TransferType = "N";
        }
    }
}