using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Services.GestionServicio.Domain;

namespace VCashApp.Services.GestionServicio.Infrastructure
{
    public class EfLocationsLookup : ILocationsLookup
    {
        private readonly AppDbContext _db;
        public EfLocationsLookup(AppDbContext db) => _db = db;

        public async Task<List<SelectListItem>> GetPointsAsync(int clientCode, int branchCode, int pointType)
            => await _db.AdmPuntos
                .Where(p => p.ClientCode == clientCode && p.BranchCode == branchCode && p.PointType == pointType && p.Status)
                .Select(p => new SelectListItem { Value = p.PointCode, Text = p.PointName ?? p.PointCode })
                .OrderBy(p => p.Text).ToListAsync();

        public async Task<List<SelectListItem>> GetFundsAsync(int clientCode, int branchCode, int fundType)
            => await _db.AdmFondos
                .Where(f => f.ClientCode == clientCode && f.BranchCode == branchCode && f.FundType == fundType && f.FundStatus)
                .Select(f => new SelectListItem { Value = f.FundCode, Text = f.FundName ?? f.FundCode })
                .OrderBy(f => f.Text).ToListAsync();

        public async Task<object?> GetLocationDetailsAsync(string code, int clientId, bool isPoint)
        {
            if (isPoint)
            {
                var p = await _db.AdmPuntos.Include(x => x.City).Include(x => x.Branch)
                    .FirstOrDefaultAsync(x => x.PointCode == code && x.ClientCode == clientId);
                return p == null ? null : new { cityName = p.City?.NombreCiudad, branchName = p.Branch?.NombreSucursal, rangeCode = p.RangeCode, rangeDetails = p.RangeAttentionInfo ?? "N/A" };
            }
            else
            {
                var f = await _db.AdmFondos.Include(x => x.City).Include(x => x.Branch)
                    .FirstOrDefaultAsync(x => x.FundCode == code && x.ClientCode == clientId);
                return f == null ? null : new { cityName = f.City?.NombreCiudad, branchName = f.Branch?.NombreSucursal, rangeCode = "N/A", rangeDetails = "N/A" };
            }
        }
    }
}
