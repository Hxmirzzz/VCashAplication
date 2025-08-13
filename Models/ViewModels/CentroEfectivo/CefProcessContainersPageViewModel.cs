using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    public class CefProcessContainersPageViewModel
    {
        [Required]
        public int CefTransactionId { get; set; }
        public ServiceHeaderVM Service { get; set; } = new();
        public TransactionHeaderVM Transaction { get; set; } = new();
        public List<CefContainerProcessingViewModel> Containers { get; set; } =
            new() { new CefContainerProcessingViewModel() };

        // Paso 3: resumen
        public decimal TotalDeclaredAll { get; set; }
        public decimal TotalCountedAll { get; set; }
        public decimal DifferenceAll { get; set; }
    }

    public class ServiceHeaderVM
    {
        public string ServiceOrderId { get; set; } = string.Empty;
        public int BranchCode { get; set; }
        public string BranchName { get; set; } = "N/A";
        public DateOnly? ServiceDate { get; set; }
        public TimeOnly? ServiceTime { get; set; }
        public string ConceptName { get; set; } = "N/A";
        public string OriginName { get; set; } = "N/A";
        public string DestinationName { get; set; } = "N/A";
        public string ClientName { get; set; } = "N/A";
    }

    public class TransactionHeaderVM
    {
        public int Id { get; set; }
        public int? SlipNumber { get; set; }
        public string? Currency { get; set; }
        public string? TransactionType { get; set; }
        public string Status { get; set; } = "N/A";
        public DateTime RegistrationDate { get; set; }
        public string RegistrationUserName { get; set; } = "N/A";
    }
}
