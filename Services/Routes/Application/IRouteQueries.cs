using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCashApp.Models.DTOs;

namespace VCashApp.Services.Routes.Application
{
    public interface IRouteQueries
    {
        Task<(IReadOnlyList<RouteListDto> items, int totalCount, int currentPage, int pageSize)>
            GetPagedAsync(RouteFilterDto filter);
        Task<RouteUpsertDto?> GetForEditAsync(string brachRouteCode);
        Task<IReadOnlyList<RouteExportDto>> ExportAsync(RouteFilterDto filter, string exportFormat);
        Task<(List<SelectListItem> Branches,
           List<SelectListItem> RouteTypes,
           List<SelectListItem> ServiceTypes,
           List<SelectListItem> VehicleTypes)>
        GetDropdownsAsync(string currentUserId, bool isAdmin);
    }
}