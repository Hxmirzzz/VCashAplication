using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Services.CentroEfectivo.Shared.Domain;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Services.CentroEfectivo.Shared.Infrastructure
{
    public sealed class CefTransactionQueries : ICefTransactionQueries
    {
        private readonly AppDbContext _db;
        private readonly IBranchContext _branchContext;

        public CefTransactionQueries(AppDbContext db, IBranchContext branchContext)
        {
            _db = db;
            _branchContext = branchContext;
        }

        public async Task<(List<SelectListItem> Sucursales, List<SelectListItem> Estados)> GetDropdownListsAsync(string currentUserId, bool isAdmin)
        {
            var allActiveBranches = await _db.AdmSucursales
                .Where(s => s.Estado && s.CodSucursal != 32)
                .Select(s => new { s.CodSucursal, s.NombreSucursal })
                .ToListAsync();

            List<SelectListItem> permittedBranchesList;
            if (!isAdmin)
            {
                var permittedBranchIds = await _db.UserClaims
                    .Where(uc => uc.UserId == currentUserId && uc.ClaimType == "SucursalId")
                    .Select(uc => int.Parse(uc.ClaimValue))
                    .ToListAsync();

                permittedBranchesList = allActiveBranches
                    .Where(s => permittedBranchIds.Contains(s.CodSucursal))
                    .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                    .ToList();
            }
            else
            {
                permittedBranchesList = allActiveBranches
                    .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                    .ToList();
            }

            var statuses = Enum.GetValues(typeof(CefTransactionStatusEnum))
                               .Cast<CefTransactionStatusEnum>()
                               .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString().Replace("_", " ") })
                               .ToList();
            statuses.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccionar Estado --" });

            return (permittedBranchesList, statuses);
        }

        public async Task<Tuple<List<CefTransactionSummaryViewModel>, int>> GetFilteredAsync(
            string currentUserId, int? branchId, DateOnly? startDate, DateOnly? endDate, CefTransactionStatusEnum? status,
            string? search, int page, int pageSize, bool isAdmin, IEnumerable<string>? conceptTypeCodes = null,
            IEnumerable<string>? excludeStatuses = null)
        {
            int? effectiveBranch = branchId ?? _branchContext.CurrentBranchId;

            DateTime? start = startDate?.ToDateTime(TimeOnly.MinValue);
            DateTime? end = endDate?.ToDateTime(TimeOnly.MaxValue);

            var txBase = _db.CefTransactions.AsNoTracking();

            var query =
                from t in txBase
                join s in _db.CgsServicios.AsNoTracking() on t.ServiceOrderId equals s.ServiceOrderId into sj
                from s in sj.DefaultIfEmpty()
                join con in _db.AdmConceptos.AsNoTracking() on s.ConceptCode equals con.CodConcepto into conj
                from con in conj.DefaultIfEmpty()
                join su in _db.AdmSucursales.AsNoTracking() on t.BranchCode equals su.CodSucursal into suj
                from su in suj.DefaultIfEmpty()
                join rd in _db.TdvRutasDiarias.AsNoTracking() on t.RouteId equals rd.Id into rdj
                from rd in rdj.DefaultIfEmpty()
                join jt in _db.AdmEmpleados.AsNoTracking() on rd.CedulaJT equals jt.CodCedula into jtj
                from jt in jtj.DefaultIfEmpty()
                select new
                {
                    T = t,
                    BranchName = su != null ? su.NombreSucursal : "",
                    HeadOfShiftName = jt != null ? jt.NombreCompleto : "",
                    ConceptType = con != null ? con.TipoConcepto : null
                };

            if (!isAdmin && effectiveBranch.HasValue) query = query.Where(x => x.T.BranchCode == effectiveBranch.Value);
            if (effectiveBranch.HasValue && isAdmin) query = query.Where(x => x.T.BranchCode == effectiveBranch.Value);
            if (start.HasValue) query = query.Where(x => x.T.RegistrationDate >= start.Value);
            if (end.HasValue) query = query.Where(x => x.T.RegistrationDate <= end.Value);
            if (status.HasValue) query = query.Where(x => x.T.TransactionStatus == status.Value.ToString());

            if (conceptTypeCodes != null && conceptTypeCodes.Any())
            {
                var codes = conceptTypeCodes.Select(c => c.Trim().ToUpper()).ToArray();
                query = query.Where(x =>
                    codes.Contains(((x.ConceptType ?? "").Trim().ToUpper()))
                    || codes.Contains(((x.T.TransactionType ?? "").Trim().ToUpper()))
                );
            }

            if (excludeStatuses != null && excludeStatuses.Any())
            {
                var ex = excludeStatuses.ToArray();
                query = query.Where(x => !ex.Contains(x.T.TransactionStatus));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                bool isNumeric = int.TryParse(term, out var slipNo);
                query = query.Where(x =>
                    x.T.ServiceOrderId!.Contains(term) ||
                    ((x.T.Currency ?? "").Contains(term)) ||
                    ((x.T.TransactionType ?? "").Contains(term)) ||
                    (isNumeric && x.T.SlipNumber == slipNo)
                );
            }

            int total = await query.CountAsync();

            var rows = await query
                .OrderByDescending(x => x.T.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CefTransactionSummaryViewModel
                {
                    Id = x.T.Id,
                    ServiceOrderId = x.T.ServiceOrderId ?? string.Empty,
                    SlipNumber = x.T.SlipNumber,
                    Currency = x.T.Currency ?? string.Empty,
                    TransactionType = x.T.TransactionType ?? (x.ConceptType ?? string.Empty),
                    TotalDeclaredValue = x.T.TotalDeclaredValue,
                    TotalCountedValue = x.T.TotalCountedValue,
                    ValueDifference = x.T.ValueDifference,
                    TransactionStatus = x.T.TransactionStatus ?? string.Empty,
                    RegistrationDate = x.T.RegistrationDate,
                    BranchName = x.BranchName,
                    HeadOfShiftName = x.HeadOfShiftName
                })
                .ToListAsync();

            return Tuple.Create(rows, total);
        }
    }
}
