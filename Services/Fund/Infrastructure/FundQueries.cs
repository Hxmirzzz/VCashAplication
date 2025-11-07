using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models.Dtos.Fund;
using VCashApp.Services.Fund.Application;
using VCashApp.Enums;
using VCashApp.Enums.Fund;

namespace VCashApp.Services.Fund.Infrastructure
{
    public class FundQueries : IFundQueries
    {
        private readonly AppDbContext _db;
        private readonly IBranchContext _branchCtx;

        public FundQueries(AppDbContext db, IBranchContext branchCtx)
        {
            _db = db;
            _branchCtx = branchCtx;
        }

        public async Task<(IReadOnlyList<FundListDto> Items, int Total, int Page, int PageSize)>
            GetPagedAsync(FundFilterDto filter)
        {
            var q = _db.AdmFondos.AsNoTracking();

            int? effectiveBranch = filter.BranchCode ?? _branchCtx.CurrentBranchId;

            if (_branchCtx.AllBranches)
            {
                if (_branchCtx.PermittedBranchIds?.Any() == true)
                    q = q.Where(f => f.BranchCode.HasValue && _branchCtx.PermittedBranchIds.Contains(f.BranchCode.Value));

                if (effectiveBranch.HasValue)
                    q = q.Where(f => f.BranchCode == effectiveBranch.Value);
            }
            else
            {
                if (_branchCtx.CurrentBranchId.HasValue)
                    q = q.Where(f => f.BranchCode == _branchCtx.CurrentBranchId.Value);
            }

            // ------ Filtros de negocio ------
            if (filter.ClientCode.HasValue)
                q = q.Where(f => f.ClientCode == filter.ClientCode.Value);

            if (filter.CityCode.HasValue)
                q = q.Where(f => f.CityCode == filter.CityCode.Value);

            if (filter.FundStatus.HasValue)
                q = q.Where(f => f.FundStatus == filter.FundStatus.Value);

            if (filter.FundType.HasValue)
                q = q.Where(f => f.FundType == filter.FundType.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim().ToLower();
                q = q.Where(f =>
                    (f.FundCode != null && f.FundCode.ToLower().Contains(s)) ||
                    (f.FundName != null && f.FundName.ToLower().Contains(s)) ||
                    (f.Cas4uCode != null && f.Cas4uCode.ToLower().Contains(s)) ||
                    (f.FundCurrency != null && f.FundCurrency.ToLower().Contains(s)) ||
                    (f.VatcoFundCode.HasValue && f.VatcoFundCode.Value.ToString().Contains(s)) ||
                    (f.Client != null && f.Client.ClientName != null && f.Client.ClientName.ToLower().Contains(s)) ||
                    (f.Branch != null && f.Branch.NombreSucursal != null && f.Branch.NombreSucursal.ToLower().Contains(s)) ||
                    (f.City != null && f.City.NombreCiudad != null && f.City.NombreCiudad.ToLower().Contains(s))
                );
            }

            var total = await q.CountAsync();
            q = q.OrderBy(f => f.FundName).ThenBy(f => f.FundCode);

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var size = filter.PageSize <= 0 ? 15 : filter.PageSize;

            var items = await q
                .Skip((page - 1) * size)
                .Take(size)
                .Select(f => new FundListDto
                {
                    FundCode = f.FundCode,
                    VatcoFundCode = f.VatcoFundCode,
                    FundName = f.FundName,

                    ClientCode = f.ClientCode,
                    ClientName = f.Client != null ? f.Client.ClientName : null,
                    BranchCode = f.BranchCode,
                    BranchName = f.Branch != null ? f.Branch.NombreSucursal : null,
                    CityCode = f.CityCode,
                    CityName = f.City != null ? f.City.NombreCiudad : null,

                    FundCurrency = f.FundCurrency,
                    FundType = f.FundType,
                    CreationDate = f.CreationDate,
                    WithdrawalDate = f.WithdrawalDate,
                    FundStatus = f.FundStatus
                })
                .ToListAsync();

            return (items, total, page, size);
        }

        public async Task<FundUpsertDto?> GetForEditAsync(string fundCode)
        {
            if (string.IsNullOrWhiteSpace(fundCode)) return null;

            var dto = await _db.AdmFondos
                .AsNoTracking()
                .Where(f => f.FundCode == fundCode)
                .Select(f => new FundUpsertDto
                {
                    FundCode = f.FundCode,
                    VatcoFundCode = f.VatcoFundCode,
                    ClientCode = f.ClientCode,
                    FundName = f.FundName,
                    BranchCode = f.BranchCode,
                    CityCode = f.CityCode,
                    CreationDate = f.CreationDate.HasValue ? f.CreationDate.Value.ToDateTime(TimeOnly.MinValue) : (System.DateTime?)null,
                    WithdrawalDate = f.WithdrawalDate.HasValue ? f.WithdrawalDate.Value.ToDateTime(TimeOnly.MinValue) : (System.DateTime?)null,
                    Cas4uCode = f.Cas4uCode,
                    FundCurrency = f.FundCurrency,
                    FundType = f.FundType,
                    FundStatus = f.FundStatus
                })
                .FirstOrDefaultAsync();

            if (dto != null && !_branchCtx.AllBranches)
            {
                if (_branchCtx.CurrentBranchId.HasValue && dto.BranchCode.HasValue)
                {
                    if (dto.BranchCode.Value != _branchCtx.CurrentBranchId.Value) return null;
                }
            }

            return dto;
        }

        public async Task<FundUpsertDto?> GetForPreviewAsync(string fundCode)
        {
            return await GetForEditAsync(fundCode);
        }

        public async Task<IReadOnlyList<FundExportDto>> ExportAsync(FundFilterDto filter)
        {
            var q = _db.AdmFondos.AsNoTracking();

            int? effectiveBranch = filter.BranchCode ?? _branchCtx.CurrentBranchId;

            if (_branchCtx.AllBranches)
            {
                if (_branchCtx.PermittedBranchIds?.Any() == true)
                    q = q.Where(f => f.BranchCode.HasValue && _branchCtx.PermittedBranchIds.Contains(f.BranchCode.Value));

                if (effectiveBranch.HasValue)
                    q = q.Where(f => f.BranchCode == effectiveBranch.Value);
            }
            else
            {
                if (_branchCtx.CurrentBranchId.HasValue)
                    q = q.Where(f => f.BranchCode == _branchCtx.CurrentBranchId.Value);
            }

            if (filter.ClientCode.HasValue)
                q = q.Where(f => f.ClientCode == filter.ClientCode.Value);

            if (filter.CityCode.HasValue)
                q = q.Where(f => f.CityCode == filter.CityCode.Value);

            if (filter.FundStatus.HasValue)
                q = q.Where(f => f.FundStatus == filter.FundStatus.Value);

            if (filter.FundType.HasValue)
                q = q.Where(f => f.FundType == filter.FundType.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim().ToLower();
                q = q.Where(f =>
                    (f.FundCode != null && f.FundCode.ToLower().Contains(s)) ||
                    (f.FundName != null && f.FundName.ToLower().Contains(s)) ||
                    (f.Cas4uCode != null && f.Cas4uCode.ToLower().Contains(s)) ||
                    (f.FundCurrency != null && f.FundCurrency.ToLower().Contains(s)) ||
                    (f.VatcoFundCode.HasValue && f.VatcoFundCode.Value.ToString().Contains(s)) ||
                    (f.Client != null && f.Client.ClientName != null && f.Client.ClientName.ToLower().Contains(s)) ||
                    (f.Branch != null && f.Branch.NombreSucursal != null && f.Branch.NombreSucursal.ToLower().Contains(s)) ||
                    (f.City != null && f.City.NombreCiudad != null && f.City.NombreCiudad.ToLower().Contains(s))
                );
            }

            q = q.OrderBy(f => f.FundName).ThenBy(f => f.FundCode);

            var rows = await q
                .Select(f => new FundExportDto
                {
                    FundCode = f.FundCode,
                    VatcoFundCode = f.VatcoFundCode,
                    FundName = f.FundName,
                    ClientName = f.Client != null ? f.Client.ClientName : null,
                    BranchName = f.Branch != null ? f.Branch.NombreSucursal : null,
                    CityName = f.City != null ? f.City.NombreCiudad : null,
                    FundCurrency = f.FundCurrency,
                    FundType = f.FundType,
                    CreationDate = f.CreationDate,
                    WithdrawalDate = f.WithdrawalDate,
                    FundStatus = f.FundStatus
                })
                .ToListAsync();

            return rows;
        }

        public async Task<FundLookupDto> GetLookupsAsync()
        {
            var clients = await _db.AdmClientes
                .AsNoTracking()
                .OrderBy(c => c.ClientName)
                .Select(c => new SelectListItem
                {
                    Value = c.ClientCode.ToString(),
                    Text = c.ClientName
                })
                .ToListAsync();

            var branchesQ = _db.AdmSucursales.AsNoTracking();

            if (_branchCtx.AllBranches)
            {
                if (_branchCtx.PermittedBranchIds?.Any() == true)
                    branchesQ = branchesQ.Where(b => _branchCtx.PermittedBranchIds.Contains(b.CodSucursal));
            }
            else
            {
                if (_branchCtx.CurrentBranchId.HasValue)
                    branchesQ = branchesQ.Where(b => b.CodSucursal == _branchCtx.CurrentBranchId.Value);
            }

            var branches = await branchesQ
                .OrderBy(b => b.NombreSucursal)
                .Select(b => new SelectListItem
                {
                    Value = b.CodSucursal.ToString(),
                    Text = b.NombreSucursal
                })
                .ToListAsync();

            var cities = await _db.AdmCiudades
                .AsNoTracking()
                .OrderBy(c => c.NombreCiudad)
                .Select(c => new SelectListItem
                {
                    Value = c.CodCiudad.ToString(),
                    Text = c.NombreCiudad
                })
                .ToListAsync();

            var currencies = Enum.GetValues(typeof(CurrencyEnum))
                .Cast<CurrencyEnum>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = e.ToString()
                })
                .ToList();
            currencies = currencies.OrderBy(c => c.Text).ToList();

            var fundTypes = Enum.GetValues(typeof(FundTypeEnum))
                .Cast<FundTypeEnum>()
                .Select(f => new SelectListItem
                {
                    Value = ((int)f).ToString(),
                    Text = f.ToString()
                })
                .ToList();
            fundTypes = fundTypes.OrderBy(c => c.Text).ToList();

            return new FundLookupDto
            {
                Clients = clients,
                Branches = branches,
                Cities = cities,
                Currencies = currencies,
                FundTypes = fundTypes
            };
        }
    }
}
