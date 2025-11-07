using VCashApp.Models.Dtos.Fund;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.Fund.Application
{
    public interface IFundService
    {
        Task<ServiceResult> CreateAsync(FundUpsertDto dto, string userId);
        Task<ServiceResult> UpdateAsync(FundUpsertDto dto, string userId);
        Task<ServiceResult> ChangeStatusAsync(string fundCode, bool newStatus, string userId);
    }
}