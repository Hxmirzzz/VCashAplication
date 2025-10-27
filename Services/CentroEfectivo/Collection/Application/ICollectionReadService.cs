using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;

namespace VCashApp.Services.CentroEfectivo.Collection.Application
{
    /// <summary>Consultas para vistas (queries) de Collection.</summary>
    public interface ICollectionReadService
    {
        Task<CefTransactionCheckinViewModel> GetCheckinAsync(string? serviceOrderId, string userId, string ipAddress);
        Task<CefProcessContainersPageViewModel?> GetProcessPageAsync(int txId);
        Task<CefTransactionDetailViewModel> GetDetailAsync(int txId);                        // Detalle de transacción
        Task<CollectionSummaryVm> GetSummaryAsync(int txId);                                 // Resumen (declared/counted/status)
        Task<CefTransactionReviewViewModel?> GetReviewAsync(int txId, string? returnUrl);

    }
}