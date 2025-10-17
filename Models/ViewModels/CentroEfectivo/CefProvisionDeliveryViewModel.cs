using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels.CentroEfectivo
{
    public sealed class CefProvisionDeliveryViewModel
    {
        public int TransactionId { get; set; }
        public string ServiceOrderId { get; set; } = string.Empty;
        public int SlipNumber { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string BranchName { get; set; } = "N/A";
        public decimal TotalCountedValue { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;

        // selección del JT
        public string? ReceiverUserId { get; set; }
        public List<SelectListItem> JtUsers { get; set; } = new();
        public string? ReturnUrl { get; set; }
    }
}
