using VCashApp.Models.ViewModels.Servicio;

namespace VCashApp.Services.GestionServicio.Domain
{
    public interface ICgsServiceQuery
    {
        Task<(List<CgsServiceSummaryViewModel> Rows, int Total)> GetFilteredAsync(
            string? search, int? clientCode, int? branchCode, int? conceptCode,
            DateOnly? startDate, DateOnly? endDate, int? status,
            int page = 1, int pageSize = 15, bool isAdmin = false);
    }
}