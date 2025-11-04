using VCashApp.Models.DTOs;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.EmployeeLog;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.EmployeeLog.Application
{
    public interface IEmployeeLogService
    {
        Task<EmployeeLogStateDto> GetEmployeeLogStatusAsync(int employeeId);

        Task<ServiceResult> RecordEmployeeEntryExitAsync(
            EmployeeLogDto logDto,
            string currentUserId,
            string? confirmedValidation = null);

        Task<ServiceResult> UpdateEmployeeLogAsync(
            int logId,
            EmployeeLogDto logDto,
            string currentUserId,
            string? confirmedValidation = null);

        Task<ServiceResult> RecordManualEmployeeExitAsync(
            int logId,
            DateOnly exitDate,
            TimeOnly exitTime,
            string currentUserId,
            string? confirmedValidation = null);

        Task<(IEnumerable<EmployeeLogListadoDto> Items, int Total)> GetFilteredEmployeeLogsAsync(
            string currentUserId,
            int? cargoId,
            string? unitId,
            int? branchId,
            DateOnly? startDate,
            DateOnly? endDate,
            int? logStatus,
            string? search,
            int page,
            int pageSize,
            bool isAdmin);

        Task<List<EmpleadoBusquedaDto>> GetEmployeeInfoAsync(
                    string userId, List<int> permittedBranchIds, string? searchInput, bool isAdmin);

        Task<EmployeeLogEditViewModel?> GetEditViewModelAsync(int id, bool canEditLog);

        Task<EmployeeLogDetailsViewModel?> GetDetailsViewModelAsync(int id);

        Task<EmployeeLogManualExitViewModel?> GetManualExitViewModelAsync(
            int id, bool canCreateLog, bool canEditLog);

        Task<EmployeeLogEntryViewModel> GetEntryViewModelAsync(
            string userName, string? unidadName, string? branchName, string? fullName, bool canCreate, bool canEdit);

        Task<ServiceResult> RecordEmployeeEntryExitAsync(
            EmployeeLogEntryViewModel vm, string currentUserId, string? confirmedValidation);

        Task<ServiceResult> UpdateEmployeeLogAsync(
            EmployeeLogEditViewModel vm, string currentUserId, string? confirmedValidation);

        Task<ServiceResult> RecordManualEmployeeExitAsync(
            EmployeeLogManualExitViewModel vm, string currentUserId, string? confirmedValidation);

        Task<SegRegistroEmpleado?> GetLogByIdAsync(int id);
    }
}