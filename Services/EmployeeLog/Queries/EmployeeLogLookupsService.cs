using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Services.EmployeeLog.Queries
{
    /// <summary>
    /// Arma los SelectList para EmployeeLog (sucursales, cargos, unidades, estados),
    /// siguiendo el patrón de CEF (el controlador no arma listas).
    /// </summary>
    public sealed class EmployeeLogLookupsService : IEmployeeLogLookupsService
    {
        private readonly AppDbContext _db;
        private readonly IBranchContext _branchContext;

        public EmployeeLogLookupsService(AppDbContext db, IBranchContext branchContext)
        {
            _db = db;
            _branchContext = branchContext;
        }

        public async Task<EmployeeLogDropdownsDto> GetDropdownListsAsync(string currentUserId, bool isAdmin)
        {
            // --------------------------
            // Sucursales visibles
            // --------------------------
            IQueryable<Models.Entities.AdmSucursal> qSuc = _db.AdmSucursales.AsNoTracking().Where(s => s.Estado);

            if (!isAdmin)
            {
                if (!_branchContext.AllBranches && _branchContext.CurrentBranchId.HasValue)
                {
                    var sid = _branchContext.CurrentBranchId.Value;
                    qSuc = qSuc.Where(s => s.CodSucursal == sid);
                }
                else
                {
                    // Todas mis sucursales (asignadas)
                    var assignedIds = await _db.UserClaims
                        .Where(uc => uc.UserId == currentUserId && uc.ClaimType == BranchClaimTypes.AssignedBranch)
                        .Select(uc => uc.ClaimValue)
                        .ToListAsync();

                    var parsed = assignedIds
                        .Select(v => int.TryParse(v, out var n) ? (int?)n : null)
                        .Where(n => n.HasValue).Select(n => n!.Value)
                        .Distinct().ToList();

                    qSuc = qSuc.Where(s => parsed.Contains(s.CodSucursal));
                }
            }

            var sucItems = (await qSuc.OrderBy(s => s.NombreSucursal).ToListAsync())
                .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                .ToList();
            sucItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona Sucursal --" });
            var sucursales = new SelectList(sucItems, "Value", "Text");

            // --------------------------
            // Cargos
            // --------------------------
            var cargosItems = (await _db.AdmCargos.AsNoTracking().OrderBy(c => c.NombreCargo).ToListAsync())
                .Select(c => new SelectListItem { Value = c.CodCargo.ToString(), Text = c.NombreCargo })
                .ToList();
            cargosItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona Cargo --" });
            var cargos = new SelectList(cargosItems, "Value", "Text");

            // --------------------------
            // Unidades
            // --------------------------
            var uniItems = (await _db.AdmUnidades.AsNoTracking().OrderBy(u => u.NombreUnidad).ToListAsync())
                .Select(u => new SelectListItem { Value = u.CodUnidad, Text = u.NombreUnidad })
                .ToList();
            uniItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona Unidad --" });
            var unidades = new SelectList(uniItems, "Value", "Text");

            // --------------------------
            // Estados del Log
            // --------------------------
            var statusItems = new[]
            {
                new SelectListItem { Value = "", Text = "-- Selecciona Estado --" },
                new SelectListItem { Value = "1", Text = "Completo" },
                new SelectListItem { Value = "0", Text = "Entrada Abierta" }
            };
            var estados = new SelectList(statusItems, "Value", "Text");

            return new EmployeeLogDropdownsDto(sucursales, cargos, unidades, estados);
        }
    }
}