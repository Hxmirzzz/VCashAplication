using VCashApp.Models.DTOs.Range;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.Range.Application
{
    public interface IRangeService
    {
        Task<ServiceResult> CreateAsync(RangeUpsertDto dto);
        Task<ServiceResult> UpdateAsync(RangeUpsertDto dto);
        Task<ServiceResult> SetStatusAsync(int id, bool active);
    }
}