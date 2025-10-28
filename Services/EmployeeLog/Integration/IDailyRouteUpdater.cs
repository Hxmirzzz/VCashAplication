using System.Threading.Tasks;

namespace VCashApp.Services.EmployeeLog.Integration
{
    public interface IDailyRouteUpdater
    {
        Task UpdateAsync(int employeeCedula, DateOnly date, TimeOnly time, bool isEntry, string currentUserId);
    }
}