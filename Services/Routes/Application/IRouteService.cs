using VCashApp.Models.DTOs;

namespace VCashApp.Services.Routes.Application
{
    public interface IRouteService
    {
        Task<(bool ok, string message)> CreateAsync(RouteUpsertDto dto, string userId);
        Task<(bool ok, string message)> UpdateAsync(RouteUpsertDto dto, string userId);
        Task<(bool ok, string message)> SetStatusAsync(string branchRouteCode, bool newStatus, string userId);
    }
}