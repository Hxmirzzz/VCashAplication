using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VCashApp.Data;
using VCashApp.Models.AdmEntities;
using VCashApp.Models.DTOs.Range;
using VCashApp.Services.DTOs;
using VCashApp.Services.Range.Application;

namespace VCashApp.Services.Range.Infrastructure
{
    public sealed class RangeQueries : IRangeQueries
    {
        private readonly AppDbContext _db;
        public RangeQueries(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<SelectListItem>> GetClientsForDropdownAsync(int? selected = null)
        {
            return await _db.AdmClientes
                .Where(c => c.Status == true || (selected.HasValue && c.ClientCode == selected.Value))
                .OrderBy(c => c.ClientName)
                .Select(c => new SelectListItem
                {
                    Value = c.ClientCode.ToString(),
                    Text = c.ClientName + (c.Status ? "" : " (inactivo)"),
                    Selected = selected.HasValue && c.ClientCode == selected.Value
                })
                .ToListAsync();
        }

        public async Task<(IReadOnlyList<RangeListDto> items, int totalCount, int currentPage, int pageSize)>
            GetPagedAsync(RangeFilterDto f)
        {
            var q = _db.AdmRangos.Include(r => r.Client).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(f.Search))
                q = q.Where(r => r.CodRange.Contains(f.Search) || (r.RangeInformation ?? "").Contains(f.Search));
            if (f.ClientId.HasValue)
                q = q.Where(r => r.ClientId == f.ClientId.Value);
            if (f.RangeStatus.HasValue)
                q = q.Where(r => r.RangeStatus == f.RangeStatus.Value);

            var total = await q.CountAsync();

            var page = f.Page <= 0 ? 1 : f.Page;
            var pageSize = f.PageSize <= 0 ? 15 : f.PageSize;

            var items = await q.OrderBy(r => r.CodRange)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(r => new RangeListDto
                               {
                                   Id = r.Id,
                                   CodRange = r.CodRange,
                                   ClientId = r.ClientId,
                                   ClientName = r.Client.ClientName,
                                   RangeInformation = r.RangeInformation,
                                   RangeStatus = r.RangeStatus,
                                   ActiveDays =
                                       (r.Monday ? "Lunes, " : "") +
                                       (r.Tuesday ? "Martes, " : "") +
                                       (r.Wednesday ? "Miercoles, " : "") +
                                       (r.Thursday ? "Jueves, " : "") +
                                       (r.Friday ? "Viernes, " : "") +
                                       (r.Saturday ? "Sabado, " : "") +
                                       (r.Sunday ? "Domingo, " : "") +
                                       (r.Holiday ? "Festivo, " : "")
                               })
                               .ToListAsync();

            return (items, total, page, pageSize);
        }

        public async Task<RangeUpsertDto?> GetForEditAsync(int id)
        {
            var r = await _db.AdmRangos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (r is null) return null;

            return new RangeUpsertDto
            {
                Id = r.Id,
                CodRange = r.CodRange,
                ClientId = r.ClientId,
                RangeInformation = r.RangeInformation,
                Monday = r.Monday,
                MondayId = r.MondayId,
                Lr1Hi = r.Lr1Hi,
                Lr1Hf = r.Lr1Hf,
                Lr2Hi = r.Lr2Hi,
                Lr2Hf = r.Lr2Hf,
                Lr3Hi = r.Lr3Hi,
                Lr3Hf = r.Lr3Hf,
                Tuesday = r.Tuesday,
                TuesdayId = r.TuesdayId,
                Mr1Hi = r.Mr1Hi,
                Mr1Hf = r.Mr1Hf,
                Mr2Hi = r.Mr2Hi,
                Mr2Hf = r.Mr2Hf,
                Mr3Hi = r.Mr3Hi,
                Mr3Hf = r.Mr3Hf,
                Wednesday = r.Wednesday,
                WednesdayId = r.WednesdayId,
                Wr1Hi = r.Wr1Hi,
                Wr1Hf = r.Wr1Hf,
                Wr2Hi = r.Wr2Hi,
                Wr2Hf = r.Wr2Hf,
                Wr3Hi = r.Wr3Hi,
                Wr3Hf = r.Wr3Hf,
                Thursday = r.Thursday,
                ThursdayId = r.ThursdayId,
                Jr1Hi = r.Jr1Hi,
                Jr1Hf = r.Jr1Hf,
                Jr2Hi = r.Jr2Hi,
                Jr2Hf = r.Jr2Hf,
                Jr3Hi = r.Jr3Hi,
                Jr3Hf = r.Jr3Hf,
                Friday = r.Friday,
                FridayId = r.FridayId,
                Vr1Hi = r.Vr1Hi,
                Vr1Hf = r.Vr1Hf,
                Vr2Hi = r.Vr2Hi,
                Vr2Hf = r.Vr2Hf,
                Vr3Hi = r.Vr3Hi,
                Vr3Hf = r.Vr3Hf,
                Saturday = r.Saturday,
                SaturdayId = r.SaturdayId,
                Sr1Hi = r.Sr1Hi,
                Sr1Hf = r.Sr1Hf,
                Sr2Hi = r.Sr2Hi,
                Sr2Hf = r.Sr2Hf,
                Sr3Hi = r.Sr3Hi,
                Sr3Hf = r.Sr3Hf,
                Sunday = r.Sunday,
                SundayId = r.SundayId,
                Dr1Hi = r.Dr1Hi,
                Dr1Hf = r.Dr1Hf,
                Dr2Hi = r.Dr2Hi,
                Dr2Hf = r.Dr2Hf,
                Dr3Hi = r.Dr3Hi,
                Dr3Hf = r.Dr3Hf,
                Holiday = r.Holiday,
                HolidayId = r.HolidayId,
                Fr1Hi = r.Fr1Hi,
                Fr1Hf = r.Fr1Hf,
                Fr2Hi = r.Fr2Hi,
                Fr2Hf = r.Fr2Hf,
                Fr3Hi = r.Fr3Hi,
                Fr3Hf = r.Fr3Hf,
                RangeStatus = r.RangeStatus
            };
        }

        public Task<AdmRange?> GetByIdAsync(int id)
    => _db.AdmRangos.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);

        public async Task<ServiceResult> ValidateUniqueAsync(RangeUpsertDto dto)
        {
            if (dto.ClientId <= 0) return ServiceResult.FailureResult("Cliente inválido.", code: "bad_request");

            var key = BuildScheduleKey(dto);

            var exists = await _db.AdmRangos
                .AsNoTracking()
                .Where(r => r.ClientId == dto.ClientId
                         && r.RangeStatus == true
                         && EF.Property<string>(r, "schedule_key") == key
                         && (!dto.Id.HasValue || r.Id != dto.Id.Value))
                .AnyAsync();

            return exists
                ? ServiceResult.FailureResult("La combinación ya existe.", code: "duplicate")
                : ServiceResult.SuccessResult("Combinación disponible.");
        }

        private static string BuildScheduleKey(RangeUpsertDto vm)
        {
            static string F(TimeSpan? t) => t.HasValue ? t.Value.ToString(@"hh\:mm") : "00:00";
            static string D(bool flag, TimeSpan? a1, TimeSpan? b1, TimeSpan? a2, TimeSpan? b2, TimeSpan? a3, TimeSpan? b3)
                => (flag ? "1" : "0") + ":" + F(a1) + "-" + F(b1) + "," + F(a2) + "-" + F(b2) + "," + F(a3) + "-" + F(b3);

            var body = string.Join("|", new[] {
                D(vm.Monday,    vm.Lr1Hi, vm.Lr1Hf, vm.Lr2Hi, vm.Lr2Hf, vm.Lr3Hi, vm.Lr3Hf),
                D(vm.Tuesday,   vm.Mr1Hi, vm.Mr1Hf, vm.Mr2Hi, vm.Mr2Hf, vm.Mr3Hi, vm.Mr3Hf),
                D(vm.Wednesday, vm.Wr1Hi, vm.Wr1Hf, vm.Wr2Hi, vm.Wr2Hf, vm.Wr3Hi, vm.Wr3Hf),
                D(vm.Thursday,  vm.Jr1Hi, vm.Jr1Hf, vm.Jr2Hi, vm.Jr2Hf, vm.Jr3Hi, vm.Jr3Hf),
                D(vm.Friday,    vm.Vr1Hi, vm.Vr1Hf, vm.Vr2Hi, vm.Vr2Hf, vm.Vr3Hi, vm.Vr3Hf),
                D(vm.Saturday,  vm.Sr1Hi, vm.Sr1Hf, vm.Sr2Hi, vm.Sr2Hf, vm.Sr3Hi, vm.Sr3Hf),
                D(vm.Sunday,    vm.Dr1Hi, vm.Dr1Hf, vm.Dr2Hi, vm.Dr2Hf, vm.Dr3Hi, vm.Dr3Hf),
                D(vm.Holiday,   vm.Fr1Hi, vm.Fr1Hf, vm.Fr2Hi, vm.Fr2Hf, vm.Fr3Hi, vm.Fr3Hf)
            });

            return $"{vm.ClientId}:{body}";
        }
    }
}