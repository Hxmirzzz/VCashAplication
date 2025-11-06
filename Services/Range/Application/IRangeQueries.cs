using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Models.DTOs.Range;
using VCashApp.Models.AdmEntities;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.Range.Application
{
    public interface IRangeQueries
    {
        Task<List<SelectListItem>> GetClientsForDropdownAsync(int? selected = null);
        Task<(IReadOnlyList<RangeListDto> items, int totalCount, int currentPage, int pageSize)>
            GetPagedAsync(RangeFilterDto f);
        Task<RangeUpsertDto?> GetForEditAsync(int id);
        Task<AdmRange?> GetByIdAsync(int id);
        Task<ServiceResult> ValidateUniqueAsync(RangeUpsertDto dto);
    }
}