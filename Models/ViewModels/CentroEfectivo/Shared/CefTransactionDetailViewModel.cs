namespace VCashApp.Models.ViewModels.CentroEfectivo.Shared
{
    public class CefTransactionDetailViewModel
    {
        // Header / Encabezado
        public int CefTransactionId { get; set; }
        public string SlipNumber { get; set; } = "";
        public string ServiceOrderId { get; set; } = "";
        public string ServiceConcept { get; set; } = "";
        public string BranchName { get; set; } = "";
        public string CurrencyCode { get; set; } = "";
        public string CurrentStatus { get; set; } = "";
        public DateTime RegistrationDate { get; set; }
        public string RegisteredByName { get; set; } = "";

        // Totales
        public decimal TotalDeclared { get; set; }
        public decimal TotalCounted { get; set; }
        public decimal NetDiff { get; set; }
        public decimal TotalOverall { get; set; } // contado + docs + cheques
        public decimal BillsHigh { get; set; }
        public decimal BillsLow { get; set; }
        public decimal BillsTotal => BillsHigh + BillsLow;
        public decimal CoinsTotal { get; set; }
        public decimal DocsTotal { get; set; }
        public decimal ChecksTotal { get; set; }

        // Bolsas / Sobres
        public List<DetailContainerVM> Containers { get; set; } = new();

        // Incidencias
        public List<DetailIncidentVM> Incidents { get; set; } = new();
    }

    public class DetailContainerVM
    {
        public int Id { get; set; }
        public string ContainerType { get; set; } = "";   // Bolsa / Sobre
        public string ContainerCode { get; set; } = "";
        public int? ParentContainerId { get; set; }
        public string ParentLabel { get; set; } = "";     // “Bolsa X — CODE”
        public decimal Subtotal { get; set; }

        public List<DetailValueRowVM> Values { get; set; } = new();
        public List<DetailCheckRowVM> Checks { get; set; } = new();
    }

    public class DetailValueRowVM
    {
        public string ValueType { get; set; } = "";        // Billete / Moneda / Documento
        public string DenominationName { get; set; } = ""; // p.ej. $50.000
        public string QualityName { get; set; } = "";      // N/A si no aplica
        public bool? IsHighDenomination { get; set; }      // null si no aplica
        public int Quantity { get; set; }
        public int Bundles { get; set; }
        public int Loose { get; set; }
        public decimal UnitValue { get; set; }
        public decimal CalculatedAmount { get; set; }
    }

    public class DetailCheckRowVM
    {
        public string BankName { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string CheckNumber { get; set; } = "";
        public DateTime? IssueDate { get; set; }
        public decimal Amount { get; set; }
    }

    public class DetailIncidentVM
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";            // Code del tipo de novedad
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";          // Reportada / Ajustada / Aprobada...
        public string ReportedBy { get; set; } = "";
        public DateTime ReportedAt { get; set; }
        public int? ContainerId { get; set; }
        public string ContainerLabel { get; set; } = "";  // “Bolsa X — CODE” si aplica
        public decimal AffectedAmount { get; set; }
        public int? AffectedDenomination { get; set; }
        public int? AffectedQuantity { get; set; }
    }
}