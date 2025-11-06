using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Models.AdmEntities;
using VCashApp.Models.DTOs.Range;
using VCashApp.Services.DTOs;
using VCashApp.Services.Range.Application;

namespace VCashApp.Services.Range.Infrastructure
{
    public sealed class RangeService : IRangeService
    {
        private readonly AppDbContext _db;
        public RangeService(AppDbContext db) => _db = db;

        public async Task<ServiceResult> CreateAsync(RangeUpsertDto dto)
        {
            var e = new AdmRange();
            MapToEntity(dto, e);

            _db.Add(e);
            try
            {
                await _db.SaveChangesAsync();
                return ServiceResult.SuccessResult("Rango creado.");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return ServiceResult.FailureResult("Ya existe un rango idéntico para este cliente.", code: "duplicate");
            }
        }

        public async Task<ServiceResult> UpdateAsync(RangeUpsertDto dto)
        {
            var e = await _db.AdmRangos.FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (e is null) return ServiceResult.FailureResult("Rango no encontrado.", code: "not_found");

            MapToEntity(dto, e);
            try
            {
                await _db.SaveChangesAsync();
                return ServiceResult.SuccessResult("Rango actualizado.");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return ServiceResult.FailureResult("Ya existe un rango idéntico para este cliente.", code: "duplicate");
            }
        }

        public async Task<ServiceResult> SetStatusAsync(int id, bool active)
        {
            var e = await _db.AdmRangos.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return ServiceResult.FailureResult("Rango no encontrado.", code: "not_found");

            e.RangeStatus = active;
            await _db.SaveChangesAsync();
            return ServiceResult.SuccessResult(active ? "Rango activado." : "Rango desactivado.");
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var sqlEx = ex.InnerException as SqlException;
            return sqlEx != null && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }

        private static TimeSpan? NullIfDisabled(bool enabled, TimeSpan? v) => enabled ? v : null;

        private static void MapToEntity(RangeUpsertDto vm, AdmRange e)
        {
            e.CodRange = vm.CodRange;
            e.ClientId = vm.ClientId;
            e.RangeInformation = vm.RangeInformation;

            e.Monday = vm.Monday; e.MondayId = vm.MondayId;
            e.Lr1Hi = NullIfDisabled(vm.Monday, vm.Lr1Hi);
            e.Lr1Hf = NullIfDisabled(vm.Monday, vm.Lr1Hf);
            e.Lr2Hi = NullIfDisabled(vm.Monday, vm.Lr2Hi);
            e.Lr2Hf = NullIfDisabled(vm.Monday, vm.Lr2Hf);
            e.Lr3Hi = NullIfDisabled(vm.Monday, vm.Lr3Hi);
            e.Lr3Hf = NullIfDisabled(vm.Monday, vm.Lr3Hf);

            e.Tuesday = vm.Tuesday; e.TuesdayId = vm.TuesdayId;
            e.Mr1Hi = NullIfDisabled(vm.Tuesday, vm.Mr1Hi); e.Mr1Hf = NullIfDisabled(vm.Tuesday, vm.Mr1Hf);
            e.Mr2Hi = NullIfDisabled(vm.Tuesday, vm.Mr2Hi); e.Mr2Hf = NullIfDisabled(vm.Tuesday, vm.Mr2Hf);
            e.Mr3Hi = NullIfDisabled(vm.Tuesday, vm.Mr3Hi); e.Mr3Hf = NullIfDisabled(vm.Tuesday, vm.Mr3Hf);

            e.Wednesday = vm.Wednesday; e.WednesdayId = vm.WednesdayId;
            e.Wr1Hi = NullIfDisabled(vm.Wednesday, vm.Wr1Hi); e.Wr1Hf = NullIfDisabled(vm.Wednesday, vm.Wr1Hf);
            e.Wr2Hi = NullIfDisabled(vm.Wednesday, vm.Wr2Hi); e.Wr2Hf = NullIfDisabled(vm.Wednesday, vm.Wr2Hf);
            e.Wr3Hi = NullIfDisabled(vm.Wednesday, vm.Wr3Hi); e.Wr3Hf = NullIfDisabled(vm.Wednesday, vm.Wr3Hf);

            e.Thursday = vm.Thursday; e.ThursdayId = vm.ThursdayId;
            e.Jr1Hi = NullIfDisabled(vm.Thursday, vm.Jr1Hi); e.Jr1Hf = NullIfDisabled(vm.Thursday, vm.Jr1Hf);
            e.Jr2Hi = NullIfDisabled(vm.Thursday, vm.Jr2Hi); e.Jr2Hf = NullIfDisabled(vm.Thursday, vm.Jr2Hf);
            e.Jr3Hi = NullIfDisabled(vm.Thursday, vm.Jr3Hi); e.Jr3Hf = NullIfDisabled(vm.Thursday, vm.Jr3Hf);

            e.Friday = vm.Friday; e.FridayId = vm.FridayId;
            e.Vr1Hi = NullIfDisabled(vm.Friday, vm.Vr1Hi); e.Vr1Hf = NullIfDisabled(vm.Friday, vm.Vr1Hf);
            e.Vr2Hi = NullIfDisabled(vm.Friday, vm.Vr2Hi); e.Vr2Hf = NullIfDisabled(vm.Friday, vm.Vr2Hf);
            e.Vr3Hi = NullIfDisabled(vm.Friday, vm.Vr3Hi); e.Vr3Hf = NullIfDisabled(vm.Friday, vm.Vr3Hf);

            e.Saturday = vm.Saturday; e.SaturdayId = vm.SaturdayId;
            e.Sr1Hi = NullIfDisabled(vm.Saturday, vm.Sr1Hi); e.Sr1Hf = NullIfDisabled(vm.Saturday, vm.Sr1Hf);
            e.Sr2Hi = NullIfDisabled(vm.Saturday, vm.Sr2Hi); e.Sr2Hf = NullIfDisabled(vm.Saturday, vm.Sr2Hf);
            e.Sr3Hi = NullIfDisabled(vm.Saturday, vm.Sr3Hi); e.Sr3Hf = NullIfDisabled(vm.Saturday, vm.Sr3Hf);

            e.Sunday = vm.Sunday; e.SundayId = vm.SundayId;
            e.Dr1Hi = NullIfDisabled(vm.Sunday, vm.Dr1Hi); e.Dr1Hf = NullIfDisabled(vm.Sunday, vm.Dr1Hf);
            e.Dr2Hi = NullIfDisabled(vm.Sunday, vm.Dr2Hi); e.Dr2Hf = NullIfDisabled(vm.Sunday, vm.Dr2Hf);
            e.Dr3Hi = NullIfDisabled(vm.Sunday, vm.Dr3Hi); e.Dr3Hf = NullIfDisabled(vm.Sunday, vm.Dr3Hf);

            e.Holiday = vm.Holiday; e.HolidayId = vm.HolidayId;
            e.Fr1Hi = NullIfDisabled(vm.Holiday, vm.Fr1Hi); e.Fr1Hf = NullIfDisabled(vm.Holiday, vm.Fr1Hf);
            e.Fr2Hi = NullIfDisabled(vm.Holiday, vm.Fr2Hi); e.Fr2Hf = NullIfDisabled(vm.Holiday, vm.Fr2Hf);
            e.Fr3Hi = NullIfDisabled(vm.Holiday, vm.Fr3Hi); e.Fr3Hf = NullIfDisabled(vm.Holiday, vm.Fr3Hf);

            e.RangeStatus = vm.RangeStatus;
        }
    }
}