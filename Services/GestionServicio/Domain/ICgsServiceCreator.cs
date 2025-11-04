using VCashApp.Models.ViewModels.Servicio;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.GestionServicio.Domain
{
    public interface ICgsServiceCreator
    {
        Task<ServiceResult> CreateAsync(CgsServiceRequestViewModel vm, string userId, string ip);
    }
}
