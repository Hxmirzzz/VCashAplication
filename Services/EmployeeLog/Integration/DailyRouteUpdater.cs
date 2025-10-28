using Microsoft.EntityFrameworkCore;
using Serilog;
using VCashApp.Data;

namespace VCashApp.Services.EmployeeLog.Integration
{
    /// <summary>
    /// Encapsula la integración con TDV_RutasDiarias para JT (cargo 64).
    /// </summary>
    public class DailyRouteUpdater : IDailyRouteUpdater
    {
        private readonly AppDbContext _context;

        public DailyRouteUpdater(AppDbContext context)
        {
            _context = context;
        }

        public async Task UpdateAsync(int employeeCedula, DateOnly date, TimeOnly time, bool isEntry, string currentUserId)
        {
            try
            {
                var employee = await _context.AdmEmpleados.Include(e => e.Cargo)
                    .FirstOrDefaultAsync(e => e.CodCedula == employeeCedula);

                if (employee == null || employee.CodCargo != 64)
                {
                    Log.Information("ROUTE_INTEGRATION: Employee {Cedula} is not JT. Skip.", employeeCedula);
                    return;
                }

                var dailyRouteAssigned = await _context.TdvRutasDiarias
                    .FirstOrDefaultAsync(r => r.CedulaJT == employeeCedula && r.FechaEjecucion == date);

                if (dailyRouteAssigned == null) return;

                if (isEntry)
                {
                    if (!dailyRouteAssigned.FechaIngresoJT.HasValue)
                    {
                        dailyRouteAssigned.FechaIngresoJT = date;
                        dailyRouteAssigned.HoraIngresoJT = time;
                    }
                }
                else
                {
                    if (!dailyRouteAssigned.FechaSalidaJT.HasValue)
                    {
                        dailyRouteAssigned.FechaSalidaJT = date;
                        dailyRouteAssigned.HoraSalidaJT = time;
                    }
                }

                _context.TdvRutasDiarias.Update(dailyRouteAssigned);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ROUTE_INTEGRATION error JT {Cedula}", employeeCedula);
            }
        }
    }
}