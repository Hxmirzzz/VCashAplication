using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels.Service
{
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
        public int ClientCode { get; set; } // Código del cliente principal (FK)

        [Display(Name = "Sucursal Principal")]
        [Required(ErrorMessage = "La sucursal principal es requerida.")]
        public int BranchCode { get; set; } // Código de la sucursal principal (FK)

        [Display(Name = "Fecha de Solicitud")]
        [Required(ErrorMessage = "La fecha de solicitud es requerida.")]
        [DataType(DataType.Date)]
        public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Display(Name = "Hora de Solicitud")]
        [Required(ErrorMessage = "La hora de solicitud es requerida.")]
        [DataType(DataType.Time)]
        public TimeOnly RequestTime { get; set; } = TimeOnly.FromDateTime(DateTime.Now);

        [Display(Name = "Concepto de Servicio")]
        [Required(ErrorMessage = "El concepto de servicio es requerido.")]
        public int ConceptCode { get; set; } // Código del concepto de servicio (FK)

        [Display(Name = "Tipo de Traslado")]
        [Required(ErrorMessage = "El tipo de traslado es requerido.")]
        [StringLength(1, ErrorMessage = "El tipo de traslado debe ser un carácter.")]
        public string? TransferType { get; set; } // Valor por defecto "N" o "I"/"T" dependiendo del ConceptCode

        [Display(Name = "Estado")]
        [Required(ErrorMessage = "El estado es requerido.")]
        public int StatusCode { get; set; } // Código del estado (FK)

        [Display(Name = "Código de Flujo")]
        public int? FlowCode { get; set; }

        // --- Origen: Campos para la selección dinámica ---
        [Display(Name = "Cliente de Origen")]
        public int? OriginClientCode { get; set; } // Cliente asociado al origen (puede ser el mismo que el principal o diferente)

        [Display(Name = "Tipo de Origen")]
        [Required(ErrorMessage = "El tipo de origen es requerido.")]
        [StringLength(1, ErrorMessage = "El tipo de origen debe ser un carácter ('P' para Punto, 'F' para Fondo).")]
        public string OriginIndicatorType { get; set; } = "P"; // 'P' (Punto/Oficina/ATM) o 'F' (Fondo)

        [Display(Name = "Punto/Fondo de Origen")]
        [Required(ErrorMessage = "El punto o fondo de origen es requerido.")]
        [StringLength(25, ErrorMessage = "El código de origen no puede exceder los 25 caracteres.")]
        public string OriginPointCode { get; set; } = null!; // Código del punto/fondo seleccionado dinámicamente

        // --- Destino: Campos para la selección dinámica ---
        [Display(Name = "Cliente de Destino")]
        public int? DestinationClientCode { get; set; } // Cliente asociado al destino

        [Display(Name = "Tipo de Destino")]
        [Required(ErrorMessage = "El tipo de destino es requerido.")]
        [StringLength(1, ErrorMessage = "El tipo de destino debe ser un carácter ('P' para Punto, 'F' para Fondo).")]
        public string DestinationIndicatorType { get; set; } = "F"; // 'P' (Punto/Oficina/ATM) o 'F' (Fondo)

        [Display(Name = "Punto/Fondo de Destino")]
        [Required(ErrorMessage = "El punto o fondo de destino es requerido.")]
        [StringLength(255, ErrorMessage = "El código de destino no puede exceder los 255 caracteres.")]
        public string DestinationPointCode { get; set; } = null!; // Código del punto/fondo seleccionado dinámicamente

        // --- Valores monetarios y cantidades ---
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
        public string? ServiceModality { get; set; } // '1' (Programado), '2' (Pedido), '3' (Frecuente)

        // --- Propiedades para SelectLists (Dropdowns) para el formulario ---
        public List<SelectListItem>? AvailableClients { get; set; } // Clientes para el dropdown principal y de origen/destino
        public List<SelectListItem>? AvailableBranches { get; set; } // Sucursales para el dropdown
        public List<SelectListItem>? AvailableConcepts { get; set; } // Conceptos de servicio
        public List<SelectListItem>? AvailableStatuses { get; set; } // Estados iniciales posibles
        public List<SelectListItem>? AvailableTransferTypes { get; set; } // Tipos de traslado (Punto a Punto, Ruta)
        public List<SelectListItem>? AvailableServiceModalities { get; set; } // Modalidades de servicio

        // Listas dinámicas para Origen/Destino (se cargarán vía AJAX)
        public List<SelectListItem>? AvailableOriginPoints { get; set; } // Puntos/ATMs de origen (dinámico)
        public List<SelectListItem>? AvailableOriginFunds { get; set; } // Fondos de origen (dinámico)
        public List<SelectListItem>? AvailableDestinationPoints { get; set; } // Puntos/ATMs de destino (dinámico)
        public List<SelectListItem>? AvailableDestinationFunds { get; set; } // Fondos de destino (dinámico)

        // --- Propiedades para mostrar información del usuario que registra (solo display) ---
        [Display(Name = "Usuario de Registro")]
        public string? CgsOperatorUserName { get; set; } // Nombre del operador que crea la solicitud (No es un campo de entrada)

        [Display(Name = "IP de Registro")]
        public string? OperatorIpAddress { get; set; } // IP del operador (No es un campo de entrada)

        // --- Propiedades adicionales que no son del formulario pero útiles para la vista (ej. para editar) ---
        public string? CurrentStatusName { get; set; } // Nombre del estado actual (si se carga para edición)
        public string? CurrentConceptName { get; set; } // Nombre del concepto actual
        public string? CurrentBranchName { get; set; } // Nombre de la sucursal actual
        public string? CurrentClientName { get; set; } // Nombre del cliente actual
        public string? CurrentOriginClientName { get; set; } // Nombre del cliente de origen
        public string? CurrentDestinationClientName { get; set; } // Nombre del cliente de destino
        public string? CurrentOriginPointName { get; set; } // Nombre del punto/fondo de origen (resuelto)
        public string? CurrentDestinationPointName { get; set; } // Nombre del punto/fondo de destino (resuelto)

        // Constructor para inicializar SelectLists vacías si es necesario
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

            TransferType = "N";
        }
    }
}