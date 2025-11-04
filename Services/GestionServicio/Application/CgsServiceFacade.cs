using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models.ViewModels.Servicio;
using VCashApp.Services;
using VCashApp.Services.GestionServicio.Domain;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.GestionServicio.Application
{
    public sealed class CgsServiceFacade : ICgsServiceApp
    {
        private readonly ICgsServiceQuery _query;
        private readonly ICgsServiceCreator _creator;
        private readonly ILocationsLookup _lookups;
        private readonly IServiceDropdownsProvider _dd;
        private readonly IBranchContext _branch;

        public CgsServiceFacade(
            ICgsServiceQuery query,
            ICgsServiceCreator creator,
            ILocationsLookup lookups,
            IServiceDropdownsProvider dd,
            IBranchContext branch)
        {
            _query = query;
            _creator = creator;
            _lookups = lookups;
            _dd = dd;
            _branch = branch;
        }

        public async Task<CgsServiceRequestViewModel> ServiceRequestAsync(string userId, string ip)
        {
            var vm = new CgsServiceRequestViewModel
            {
                AvailableClients = await _dd.ClientsAsync(),
                AvailableBranches = await _dd.BranchesAsync(),
                AvailableConcepts = await _dd.ConceptsAsync(),
                AvailableStatuses = await _dd.StatusesAsync(),
                AvailableOriginTypes = _dd.LocationTypes(),
                AvailableDestinationTypes = _dd.LocationTypes(),
                AvailableTransferTypes = new List<SelectListItem> {
                    new("Normal (Predeterminado)","N"),
                    new("Interno","I"),
                    new("Transportadora","T")
                },
                AvailableFailedResponsibles = await _dd.FailedResponsiblesAsync(),
                AvailableServiceModalities = await _dd.ServiceModalitiesAsync(),
                AvailableCurrencies = _dd.Currencies(),
                Currency = "COP",
                OperatorIpAddress = ip
            };

            if (_branch.CurrentBranchId.HasValue)
            {
                vm.BranchCode = _branch.CurrentBranchId.Value;
                vm.OperatorBranchName = (await _dd.BranchesAsync())
                    .FirstOrDefault(x => x.Value == _branch.CurrentBranchId.Value.ToString())?.Text ?? "N/A";
            }
            else vm.OperatorBranchName = "N/A";

            return vm;
        }

        public async Task<Tuple<List<CgsServiceSummaryViewModel>, int>> GetFilteredServiceRequestsAsync(
            string? search, int? clientCode, int? branchCode, int? conceptCode,
            DateOnly? startDate, DateOnly? endDate, int? status,
            int page = 1, int pageSize = 15, string? currentUserId = null, bool isAdmin = false)
        {
            var (rows, total) = await _query.GetFilteredAsync(
                search, clientCode, branchCode, conceptCode, startDate, endDate, status, page, pageSize, isAdmin);
            return Tuple.Create(rows, total);
        }

        public Task<ServiceResult> CreateServiceRequestAsync(CgsServiceRequestViewModel vm, string userId, string ip)
            => _creator.CreateAsync(vm, userId, ip);

        public Task<List<SelectListItem>> GetClientsForDropdownAsync() => _dd.ClientsAsync();
        public Task<List<SelectListItem>> GetBranchesForDropdownAsync() => _dd.BranchesAsync();
        public Task<List<SelectListItem>> GetServiceConceptsForDropdownAsync() => _dd.ConceptsAsync();
        public Task<List<SelectListItem>> GetServiceStatusesForDropdownAsync() => _dd.StatusesAsync();
        public Task<List<SelectListItem>> GetServiceModalitiesForDropdownAsync() => _dd.ServiceModalitiesAsync();
        public Task<List<SelectListItem>> GetFailedResponsiblesForDropdown() => _dd.FailedResponsiblesAsync();

        public Task<List<SelectListItem>> GetPointsByClientAndBranchAsync(int clientCode, int branchCode, int pointType)
            => _lookups.GetPointsAsync(clientCode, branchCode, pointType);

        public Task<List<SelectListItem>> GetFundsByClientAndBranchAsync(int clientCode, int branchCode, int fundType)
            => _lookups.GetFundsAsync(clientCode, branchCode, fundType);

        public Task<object?> GetLocationDetailsByCodeAsync(string code, int clientId, bool isPoint)
            => _lookups.GetLocationDetailsAsync(code, clientId, isPoint);

        public Task<List<SelectListItem>> GetCurrenciesForDropdownAsync() => Task.FromResult(_dd.Currencies());
    }
}
