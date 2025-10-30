using VCashApp.Models.DTOs;
using VCashApp.Models.Entities;

namespace VCashApp.Services.Employee.Domain
{
    public interface IEmployeeRepository
    {
        Task<(IEnumerable<EmpleadoListadoDto> Items, int Total)> SearchAsync(
            int? cargoId, int? branchId, int? employeeStatus,
            string? search, string? gender, int page, int pageSize,
            bool allBranches, int? currentBranchId, IEnumerable<int> permittedBranches);

        Task<AdmEmpleado?> GetByIdAsync(int codCedula);

        Task AddAsync(AdmEmpleado entity);
        Task UpdateAsync(AdmEmpleado entity);

        Task SaveChangesAsync();

        Task<IEnumerable<(int Value, string Text)>> GetCargosAsync();
        Task<IEnumerable<(int Value, string Text)>> GetSucursalesAsync(
            bool allBranches, int? currentBranchId, IEnumerable<int> permittedBranches);
        Task<IEnumerable<(int Value, string Text)>> GetCiudadesAsync();

        Task<List<EmpleadoExportDto>> ExportAsync(
            int? cargoId, int? branchId, int? employeeStatus,
            string? search, string? gender,
            bool allBranches, int? currentBranchId, IEnumerable<int> permittedBranches);
    }
}