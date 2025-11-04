using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models.ViewModels.Servicio;
using VCashApp.Services.GestionServicio.Domain;

namespace VCashApp.Services.GestionServicio.Infrastructure
{
    public sealed class EfCgsServiceQuery : ICgsServiceQuery
    {
        private readonly AppDbContext _db;
        private readonly IBranchContext _branch;

        public EfCgsServiceQuery(AppDbContext db, IBranchContext branch)
        {
            _db = db;
            _branch = branch;
        }

        public async Task<(List<CgsServiceSummaryViewModel> Rows, int Total)> GetFilteredAsync(
            string? search, int? clientCode, int? branchCode, int? conceptCode,
            DateOnly? startDate, DateOnly? endDate, int? status,
            int page = 1, int pageSize = 15, bool isAdmin = false)
        {
            int? effectiveBranch = branchCode ?? _branch.CurrentBranchId;

            var q =
                from s in _db.CgsServicios.AsNoTracking()
                join c in _db.AdmClientes.AsNoTracking() on s.ClientCode equals c.ClientCode into cj
                from c in cj.DefaultIfEmpty()
                join b in _db.AdmSucursales.AsNoTracking() on s.BranchCode equals b.CodSucursal into bj
                from b in bj.DefaultIfEmpty()
                join cc in _db.AdmConceptos.AsNoTracking() on s.ConceptCode equals cc.CodConcepto into ccj
                from cc in ccj.DefaultIfEmpty()
                join st in _db.AdmEstados.AsNoTracking() on s.StatusCode equals st.StateCode into stj
                from st in stj.DefaultIfEmpty()
                select new { s, ClientName = c!.ClientName, BranchName = b!.NombreSucursal, ConceptName = cc!.NombreConcepto, StatusName = st!.StateName };

            if (!isAdmin)
            {
                if (_branch.AllBranches)
                    q = q.Where(x => _branch.PermittedBranchIds.Contains(x.s.BranchCode));
                else if (effectiveBranch.HasValue)
                    q = q.Where(x => x.s.BranchCode == effectiveBranch.Value);
            }
            else if (effectiveBranch.HasValue)
            {
                q = q.Where(x => x.s.BranchCode == effectiveBranch.Value);
            }

            if (clientCode.HasValue) q = q.Where(x => x.s.ClientCode == clientCode.Value);
            if (conceptCode.HasValue) q = q.Where(x => x.s.ConceptCode == conceptCode.Value);
            if (status.HasValue) q = q.Where(x => x.s.StatusCode == status.Value);
            if (startDate.HasValue) q = q.Where(x => x.s.RequestDate >= startDate.Value);
            if (endDate.HasValue) q = q.Where(x => x.s.RequestDate <= endDate.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                q = q.Where(x =>
                    x.s.ServiceOrderId.Contains(term) ||
                    (x.ClientName ?? "").Contains(term) ||
                    (x.BranchName ?? "").Contains(term) ||
                    (x.ConceptName ?? "").Contains(term));
            }

            var total = await q.CountAsync();

            var rows = await q
                .OrderByDescending(x => x.s.RequestDate)
                .ThenByDescending(x => x.s.RequestTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CgsServiceSummaryViewModel
                {
                    ServiceOrderId = x.s.ServiceOrderId,
                    KeyValue = x.s.KeyValue ?? 0,
                    ClientName = x.ClientName ?? string.Empty,
                    BranchName = x.BranchName ?? string.Empty,
                    OriginPointName = x.s.OriginPointCode,
                    DestinationPointName = x.s.DestinationPointCode,
                    ConceptName = x.ConceptName ?? string.Empty,
                    RequestDate = x.s.RequestDate,
                    RequestTime = x.s.RequestTime,
                    ProgrammingDate = x.s.ProgrammingDate,
                    ProgrammingTime = x.s.ProgrammingTime,
                    StatusCode = x.s.StatusCode,
                    StatusName = x.StatusName ?? string.Empty
                })
                .ToListAsync();

            return (rows, total);
        }
    }
}
