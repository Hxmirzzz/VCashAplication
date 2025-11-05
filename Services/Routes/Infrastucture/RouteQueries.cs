using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Models.Entities;
using VCashApp.Models.DTOs;
using static VCashApp.Enums.RouteCatalogs;
using VCashApp.Enums;
using VCashApp.Services.Routes.Application;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Services.Routes.Infrastucture
{
    public class RouteQueries : IRouteQueries
    {
        private readonly AppDbContext _db;
        private readonly IBranchContext _branchCtx;

        public RouteQueries(AppDbContext db, IBranchContext branchCtx)
        {
            _db = db;
            _branchCtx = branchCtx;
        }

        public async Task<(IReadOnlyList<RouteListDto> items, int totalCount, int currentPage, int pageSize)>
            GetPagedAsync(RouteFilterDto filter)
        {
            var q = _db.Set<AdmRoute>().AsNoTracking();

            int? effectiveBranch = filter.BranchId ?? _branchCtx.CurrentBranchId;

            if (_branchCtx.AllBranches)
            {
                if (_branchCtx.PermittedBranchIds?.Any() == true)
                    q = q.Where(r => r.BranchId.HasValue && _branchCtx.PermittedBranchIds.Contains(r.BranchId.Value));

                if (effectiveBranch.HasValue)
                    q = q.Where(r => r.BranchId == effectiveBranch.Value);
            }
            else
            {
                if (effectiveBranch.HasValue)
                    q = q.Where(r => r.BranchId == effectiveBranch.Value);
                else
                {
                    if (_branchCtx.PermittedBranchIds?.Any() == true)
                        q = q.Where(r => r.BranchId.HasValue && _branchCtx.PermittedBranchIds.Contains(r.BranchId.Value));
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                q = q.Where(r =>
                    (r.RouteCode ?? "").Contains(s) ||
                    (r.RouteName ?? "").Contains(s));
            }

            if (filter.BranchId.HasValue)
                q = q.Where(r => r.BranchId == filter.BranchId);

            if (!string.IsNullOrWhiteSpace(filter.RouteType))
                q = q.Where(r => r.RouteType == filter.RouteType);

            if (!string.IsNullOrWhiteSpace(filter.ServiceType))
                q = q.Where(r => r.ServiceType == filter.ServiceType);

            if (!string.IsNullOrWhiteSpace(filter.VehicleType))
                q = q.Where(r => r.VehicleType == filter.VehicleType);

            if (filter.IsActive.HasValue)
            {
                if (filter.IsActive.Value is 0 or 1)
                {
                    bool val = filter.IsActive.Value == 1;
                    q = q.Where(r => r.Status == val);
                }
            }

            var total = await q.CountAsync();

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 15 : filter.PageSize;

            var items = await q
                .OrderBy(r => r.RouteName ?? r.RouteCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RouteListDto(
                    r.BranchRouteCode,
                    r.RouteCode,
                    r.RouteName,
                    r.BranchId,
                    r.Branch != null ? r.Branch.NombreSucursal : null,
                    r.RouteType,
                    r.ServiceType,
                    r.VehicleType,
                    r.Amount,
                    r.Status
                ))
                .ToListAsync();

            return (items, total, page, pageSize);
        }

        public async Task<RouteUpsertDto?> GetForEditAsync(string branchRouteCode)
        {
            var r = await _db.Set<AdmRoute>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchRouteCode == branchRouteCode);

            if (r is null) return null;

            if (!_branchCtx.AllBranches)
            {
                if (_branchCtx.CurrentBranchId.HasValue && r.BranchId != _branchCtx.CurrentBranchId)
                    return null;
                if (!_branchCtx.CurrentBranchId.HasValue && _branchCtx.PermittedBranchIds?.Any() == true
                    && (!r.BranchId.HasValue || !_branchCtx.PermittedBranchIds.Contains(r.BranchId.Value)))
                    return null;
            }

            return new RouteUpsertDto
            {
                BranchRouteCode = r.BranchRouteCode,
                RouteCode = r.RouteCode,
                RouteName = r.RouteName,
                BranchId = r.BranchId,
                RouteType = r.RouteType,
                ServiceType = r.ServiceType,
                VehicleType = r.VehicleType,
                Amount = r.Amount,
                IsActive = r.Status,

                Monday = r.Monday,
                MondayStartTime = r.MondayStartTime,
                MondayEndTime = r.MondayEndTime,

                Tuesday = r.Tuesday,
                TuesdayStartTime = r.TuesdayStartTime,
                TuesdayEndTime = r.TuesdayEndTime,

                Wednesday = r.Wednesday,
                WednesdayStartTime = r.WednesdayStartTime,
                WednesdayEndTime = r.WednesdayEndTime,

                Thursday = r.Thursday,
                ThursdayStartTime = r.ThursdayStartTime,
                ThursdayEndTime = r.ThursdayEndTime,

                Friday = r.Friday,
                FridayStartTime = r.FridayStartTime,
                FridayEndTime = r.FridayEndTime,

                Saturday = r.Saturday,
                SaturdayStartTime = r.SaturdayStartTime,
                SaturdayEndTime = r.SaturdayEndTime,

                Sunday = r.Sunday,
                SundayStartTime = r.SundayStartTime,
                SundayEndTime = r.SundayEndTime,

                Holiday = r.Holiday,
                HolidayStartTime = r.HolidayStartTime,
                HolidayEndTime = r.HolidayEndTime
            };
        }

        public async Task<IReadOnlyList<RouteExportDto>> ExportAsync(RouteFilterDto filter, string exportFormat)
        {
            var (items, _, _, _) = await GetPagedAsync(new RouteFilterDto
            {
                Page = 1,
                PageSize = int.MaxValue,
                Search = filter.Search,
                BranchId = filter.BranchId,
                RouteType = filter.RouteType,
                ServiceType = filter.ServiceType,
                VehicleType = filter.VehicleType,
                IsActive = filter.IsActive
            });

            var branchIds = items.Select(i => i.BranchId).Distinct().ToList();
            var branchNames = await _db.AdmSucursales
                .Where(s => items.Select(i => i.BranchId).Distinct().Contains(s.CodSucursal))
                .Select(s => new { s.CodSucursal, s.NombreSucursal })
                .ToDictionaryAsync(k => (int?)k.CodSucursal, v => v.NombreSucursal ?? "");

            return items.Select(i =>
                    new RouteExportDto(
                        RouteCode: i.RouteCode ?? "",
                        RouteName: i.RouteName ?? "",
                        Branch: branchNames.TryGetValue(i.BranchId, out var name) ? name : "",
                        RouteType: RouteTypeCode.ToLabel(i.RouteType),
                        ServiceType: ServiceTypeCode.ToLabel(i.ServiceType),
                        VehicleType: VehicleTypeCode.ToLabel(i.VehicleType),
                        Amount: i.Amount,
                        Status: i.IsActive ? "ACTIVO" : "INACTIVO"
                    )
                ).ToList();
        }

        public async Task<(List<SelectListItem> Branches,
                   List<SelectListItem> RouteTypes,
                   List<SelectListItem> ServiceTypes,
                   List<SelectListItem> VehicleTypes)>
            GetDropdownsAsync(string currentUserId, bool isAdmin)
        {
            var allActiveBranches = await _db.AdmSucursales
                .AsNoTracking()
                .Where(s => s.Estado && s.CodSucursal != 32)
                .Select(s => new { s.CodSucursal, s.NombreSucursal })
                .ToListAsync();

            IEnumerable<int> permitted = _branchCtx.AllBranches
                ? (_branchCtx.PermittedBranchIds?.Any() == true
                    ? _branchCtx.PermittedBranchIds
                    : allActiveBranches.Select(b => b.CodSucursal))
                : (_branchCtx.CurrentBranchId.HasValue
                    ? new[] { _branchCtx.CurrentBranchId.Value }
                    : (_branchCtx.PermittedBranchIds ?? Enumerable.Empty<int>()));

            var branchOptions = allActiveBranches
                .Where(b => !permitted.Any() || permitted.Contains(b.CodSucursal))
                .Select(b => new SelectListItem { Value = b.CodSucursal.ToString(), Text = b.NombreSucursal })
                .ToList();
            branchOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Todas --" });

            static List<SelectListItem> BuildRouteTypeOptions() => new()
            {
                new SelectListItem { Value = "", Text = "-- Tipo de Ruta --" },
                new SelectListItem { Value = RouteTypeCode.Traditional,     Text = RouteTypeCode.ToLabel(RouteTypeCode.Traditional) },
                new SelectListItem { Value = RouteTypeCode.ATM,             Text = RouteTypeCode.ToLabel(RouteTypeCode.ATM) },
                new SelectListItem { Value = RouteTypeCode.Mixed,           Text = RouteTypeCode.ToLabel(RouteTypeCode.Mixed) },
                new SelectListItem { Value = RouteTypeCode.CashLiberation,  Text = RouteTypeCode.ToLabel(RouteTypeCode.CashLiberation) },
            };

            static List<SelectListItem> BuildServiceTypeOptions() => new()
            {
                new SelectListItem { Value = "", Text = "-- Jornada --" },
                new SelectListItem { Value = ServiceTypeCode.Morning,    Text = ServiceTypeCode.ToLabel(ServiceTypeCode.Morning) },
                new SelectListItem { Value = ServiceTypeCode.Afternoon,  Text = ServiceTypeCode.ToLabel(ServiceTypeCode.Afternoon) },
                new SelectListItem { Value = ServiceTypeCode.Additional, Text = ServiceTypeCode.ToLabel(ServiceTypeCode.Additional) },
            };

                    static List<SelectListItem> BuildVehicleTypeOptions() => new()
            {
                new SelectListItem { Value = "", Text = "-- Vehículo --" },
                new SelectListItem { Value = VehicleTypeCode.Armored,    Text = VehicleTypeCode.ToLabel(VehicleTypeCode.Armored) },
                new SelectListItem { Value = VehicleTypeCode.Motorcycle, Text = VehicleTypeCode.ToLabel(VehicleTypeCode.Motorcycle) },
                new SelectListItem { Value = VehicleTypeCode.Van,        Text = VehicleTypeCode.ToLabel(VehicleTypeCode.Van) },
                new SelectListItem { Value = VehicleTypeCode.Truck,      Text = VehicleTypeCode.ToLabel(VehicleTypeCode.Truck) },
            };

            return (branchOptions, BuildRouteTypeOptions(), BuildServiceTypeOptions(), BuildVehicleTypeOptions());
        }
    }
}
