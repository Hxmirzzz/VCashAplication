using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Services.GestionServicio.Domain
{
    public interface IServiceDropdownsProvider
    {
        Task<List<SelectListItem>> ClientsAsync();
        Task<List<SelectListItem>> BranchesAsync();
        Task<List<SelectListItem>> ConceptsAsync();
        Task<List<SelectListItem>> StatusesAsync();
        Task<List<SelectListItem>> ServiceModalitiesAsync();
        List<SelectListItem> Currencies();
        List<SelectListItem> LocationTypes();
        Task<List<SelectListItem>> FailedResponsiblesAsync();
    }
}
