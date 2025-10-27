using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Services.CentroEfectivo.Shared.Domain
{
    public interface ICefCatalogRepository
    {
        Task<string> BuildDenomsJsonForTransactionAsync(int txId);
        Task<string> BuildQualitiesJsonAsync();
        Task<string> BuildBankEntitiesJsonAsync();
        Task<List<SelectListItem>> GetCurrenciesForDropdownAsync();
    }
}