using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    /// <summary>
    /// ViewModel que agrupa la información necesaria para procesar contenedores
    /// de una transacción de efectivo.
    /// </summary>
    public class CefProcessContainersPageViewModel
    {
        /// <summary>
        /// Identificador de la transacción de efectivo asociada.
        /// </summary>
        [Required]
        public int CefTransactionId { get; set; }

        /// <summary>
        /// Información general del servicio relacionado.
        /// </summary>
        public ServiceHeaderVM Service { get; set; } = new();

        /// <summary>
        /// Datos principales de la transacción.
        /// </summary>
        public TransactionHeaderVM Transaction { get; set; } = new();

        /// <summary>
        /// Contenedores a procesar dentro de la transacción.
        /// </summary>
        public List<CefContainerProcessingViewModel> Containers { get; set; } =
            new() { new CefContainerProcessingViewModel() };

        /// <summary>
        /// Total de valores declarados en todos los contenedores.
        /// </summary>
        public decimal TotalDeclaredAll { get; set; }

        /// <summary>
        /// Total de valores contados en todos los contenedores.
        /// </summary>
        public decimal TotalCountedAll { get; set; }

        /// <summary>
        /// Diferencia total entre valores declarados y contados.
        /// </summary>
        public decimal DifferenceAll { get; set; }

        public decimal TotalOverallAll { get; set; }          // efectivo + cheques + documentos
        public decimal CountedBillsAll { get; set; }          // solo billetes (alto+bajo)
        public decimal CountedBillHighAll { get; set; }       // billete alta denominación
        public decimal CountedBillLowAll { get; set; }        // billete baja denominación
        public decimal CountedCoinsAll { get; set; }          // monedas
        public decimal CountedDocsAll { get; set; }           // documentos
        public decimal CountedChecksAll { get; set; }         // cheques

        public string? Currency { get; set; }                    // valor actual (ej. "COP")
        public IEnumerable<SelectListItem> Currencies { get; set; } = Enumerable.Empty<SelectListItem>();

    }

    /// <summary>
    /// Datos de encabezado del servicio asociado a la transacción.
    /// </summary>
    public class ServiceHeaderVM
    {
        /// <summary>
        /// Número de orden del servicio.
        /// </summary>
        public string ServiceOrderId { get; set; } = string.Empty;

        /// <summary>
        /// Código de la sucursal donde se registró el servicio.
        /// </summary>
        public int BranchCode { get; set; }

        /// <summary>
        /// Nombre de la sucursal asociada.
        /// </summary>
        public string BranchName { get; set; } = "N/A";

        /// <summary>
        /// Fecha programada del servicio.
        /// </summary>
        public DateOnly? ServiceDate { get; set; }

        /// <summary>
        /// Hora programada del servicio.
        /// </summary>
        public TimeOnly? ServiceTime { get; set; }

        /// <summary>
        /// Nombre del concepto del servicio.
        /// </summary>
        public string ConceptName { get; set; } = "N/A";

        /// <summary>
        /// Nombre del origen del servicio.
        /// </summary>
        public string OriginName { get; set; } = "N/A";

        /// <summary>
        /// Nombre del destino del servicio.
        /// </summary>
        public string DestinationName { get; set; } = "N/A";

        /// <summary>
        /// Nombre del cliente asociado al servicio.
        /// </summary>
        public string ClientName { get; set; } = "N/A";
    }

    /// <summary>
    /// Datos de encabezado de la transacción de efectivo.
    /// </summary>
    public class TransactionHeaderVM
    {
        /// <summary>
        /// Identificador único de la transacción.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Número de volante o comprobante, si aplica.
        /// </summary>
        public int? SlipNumber { get; set; }

        /// <summary>
        /// Moneda en la que se realiza la transacción.
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Tipo de transacción realizada.
        /// </summary>
        public string? TransactionType { get; set; }

        /// <summary>
        /// Estado actual de la transacción.
        /// </summary>
        public string Status { get; set; } = "N/A";

        /// <summary>
        /// Fecha y hora de registro de la transacción.
        /// </summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// Usuario que registró la transacción.
        /// </summary>
        public string RegistrationUserName { get; set; } = "N/A";
    }
}
