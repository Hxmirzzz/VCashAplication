using System.Collections.Generic;
using System.Threading.Tasks;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Services.CentroEfectivo.Provision.Domain;

namespace VCashApp.Services.CentroEfectivo.Provision.Infrastructure
{
    public interface ICefContainerRepository
    {
        Task SaveContainersAndDetailsAsync(
            int txId,
            IReadOnlyList<CefContainerProcessingViewModel> containers,
            string userId,
            IAllowedValueTypesPolicy allowed);
    }
}