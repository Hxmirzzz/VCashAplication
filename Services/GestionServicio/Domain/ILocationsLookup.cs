using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Services.GestionServicio.Domain
{
    public interface ILocationsLookup
    {
        Task<List<SelectListItem>> GetPointsAsync(int clientCode, int branchCode, int pointType);
        Task<List<SelectListItem>> GetFundsAsync (int clientCode, int branchCode, int fundType);
        Task<object?> GetLocationDetailsAsync(string code, int clientId, bool isPoint);
    }
}