using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.Employee;
using VCashApp.Services.DTOs;
using VCashApp.Services.Employee.Domain;
using VCashApp.Services.Employee.Infrastructure;

namespace VCashApp.Services.Employee.Application
{
    public class EmployeeWriteService : IEmployeeWriteService
    {
        private readonly IEmployeeRepository _repo;
        private readonly IEmployeeFileStorage _storage;

        public EmployeeWriteService(IEmployeeRepository repo, IEmployeeFileStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<ServiceResult> CreateAsync(EmployeeViewModel m, string userId)
        {
            if (m.CodCedula > int.MaxValue)
                return ServiceResult.FailureResult("La cédula excede el máximo permitido.");

            var e = new AdmEmpleado { CodCedula = (int)m.CodCedula };
            e.UpdateEntity(m);

            await SaveFilesAsync(m, e);
            await _repo.AddAsync(e);
            await _repo.SaveChangesAsync();
            return ServiceResult.SuccessResult(
                "Empleado creado correctamente.",
                id: e.CodCedula,
                userId: userId
            );
        }

        public async Task<ServiceResult> UpdateAsync (EmployeeViewModel m, string userId)
        {
            var e = await _repo.GetByIdAsync((int)m.CodCedula);
            if (e == null)
                return ServiceResult.FailureResult("Empleado no encontrado.");

            e.UpdateEntity(m);
            await SaveFilesAsync(m, e);
            await _repo.UpdateAsync(e);
            await _repo.SaveChangesAsync();
            return ServiceResult.SuccessResult(
                "Empleado actualizado correctamente.",
                id: e.CodCedula,
                userId: userId
            );
        }

        public async Task<ServiceResult> ChangeStatusAsync(int id, int newStatus, string reason, string userId)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null)
                return ServiceResult.FailureResult("Empleado no encontrado.");

            e.EmpleadoEstado = (EstadoEmpleado?)newStatus;

            await _repo.UpdateAsync(e);
            await _repo.SaveChangesAsync();

            return ServiceResult.SuccessResult(
                "Estado del empleado actualizado correctamente.",
                id: e.CodCedula,
                userId: userId
            );
        }

        public Task<Stream?> OpenImageAsync(string relativePath)
            => _storage.OpenReadAsync(relativePath);

        private async Task SaveFilesAsync(EmployeeViewModel m, AdmEmpleado e)
        {
            if (m.PhotoFile != null && m.PhotoFile.Length > 0)
            {
                using var s = m.PhotoFile.OpenReadStream();
                e.FotoUrl = await _storage.SaveAsync("Empleados/Fotos", $"{e.CodCedula}P{Path.GetExtension(m.PhotoFile.FileName)}", s);
            }

            if (m.SignatureFile != null && m.SignatureFile.Length > 0)
            {
                using var s = m.SignatureFile.OpenReadStream();
                e.FirmaUrl = await _storage.SaveAsync("Empleados/Firmas", $"{e.CodCedula}F{Path.GetExtension(m.SignatureFile.FileName)}", s);
            }
        }
    }
}
