using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Models.ViewModels.Servicio;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.GestionServicio.Application
{
    public interface ICgsServiceApp
    {
        Task<CgsServiceRequestViewModel> ServiceRequestAsync(string userId, string ip);

        Task<Tuple<List<CgsServiceSummaryViewModel>, int>> GetFilteredServiceRequestsAsync(
            string? search, int? clientCode, int? branchCode, int? conceptCode,
            DateOnly? startDate, DateOnly? endDate, int? status,
            int page = 1, int pageSize = 15, string? currentUserId = null, bool isAdmin = false);

        Task<ServiceResult> CreateServiceRequestAsync(CgsServiceRequestViewModel vm, string userId, string ip);
        Task<List<SelectListItem>> GetClientsForDropdownAsync();
        Task<List<SelectListItem>> GetBranchesForDropdownAsync();
        Task<List<SelectListItem>> GetServiceConceptsForDropdownAsync();
        Task<List<SelectListItem>> GetServiceStatusesForDropdownAsync();
        Task<List<SelectListItem>> GetServiceModalitiesForDropdownAsync();
        Task<List<SelectListItem>> GetFailedResponsiblesForDropdown();
        Task<List<SelectListItem>> GetPointsByClientAndBranchAsync(int clientCode, int branchCode, int pointType);
        Task<List<SelectListItem>> GetFundsByClientAndBranchAsync(int clientCode, int branchCode, int fundType);
        Task<object?> GetLocationDetailsByCodeAsync(string code, int clientId, bool isPoint);
        Task<List<SelectListItem>> GetCurrenciesForDropdownAsync();
    }
}