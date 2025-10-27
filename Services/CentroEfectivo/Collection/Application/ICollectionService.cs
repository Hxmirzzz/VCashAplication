using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;

namespace VCashApp.Services.CentroEfectivo.Collection.Application
{
    /// <summary>Casos de uso de Recolección (commands).</summary>
    public interface ICollectionService
    {
        Task<int> CreateCollectionAsync(CreateCollectionCmd cmd, string userId);                 // check-in / creación
        Task SaveContainersAsync(int txId, SaveCollectionContainersCmd cmd, string userId);      // guardar bolsas/sobres
        Task FinalizeAsync(int txId, string userId);                                             // cierre (según política/estado)
        Task ApproveAsync(int txId, string reviewerUserId);                                      // aprobación supervisor
        Task<bool> RecalcTotalsAndNetDiffAsync(int txId);
    }
}
