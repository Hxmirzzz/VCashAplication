using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;

namespace VCashApp.Services.CentroEfectivo.Shared.Domain
{
    public interface ICefTransactionQueries
    {
        Task<(List<SelectListItem> Sucursales, List<SelectListItem> Estados)> GetDropdownListsAsync(string currentUserId, bool isAdmin);
        Task<Tuple<List<CefTransactionSummaryViewModel>, int>> GetFilteredAsync(
            string currentUserId, int? branchId, DateOnly? startDate, DateOnly? endDate, CefTransactionStatusEnum? status,
            string? search, int page, int pageSize, bool isAdmin, IEnumerable<string>? conceptTypeCodes = null,
            IEnumerable<string>? excludeStatuses = null);
    }
}
