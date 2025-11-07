using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models.Dtos.Fund;
using VCashApp.Models.Entities;
using VCashApp.Services.DTOs;
using VCashApp.Services.Fund.Application;

namespace VCashApp.Services.Fund.Infrastructure
{
    public class FundService : IFundService
    {
        private readonly AppDbContext _db;
        private readonly IBranchContext _branchCtx;

        public FundService(AppDbContext db, IBranchContext branchCtx)
        {
            _db = db;
            _branchCtx = branchCtx;
        }

        public async Task<ServiceResult> CreateAsync(FundUpsertDto dto, string userId)
        {
            var errors = ValidateDto(dto, isCreate: true);
            if (errors is { Count: > 0 })
                return ServiceResult.FailureResult("Validación", errors: ToErrors(errors));

            var exists = await _db.AdmFondos.AsNoTracking().AnyAsync(f => f.FundCode == dto.FundCode);
            if (exists)
                return ServiceResult.FailureResult("Validación", errors: ToErrors("Ya existe un fondo con el mismo código."));
            Normalize(dto);

            if (!_branchCtx.AllBranches && !dto.BranchCode.HasValue && _branchCtx.CurrentBranchId.HasValue)
                dto.BranchCode = _branchCtx.CurrentBranchId.Value;

            var entity = new AdmFondo
            {
                FundCode = dto.FundCode,
                VatcoFundCode = dto.VatcoFundCode,
                ClientCode = dto.ClientCode,
                FundName = dto.FundName,
                BranchCode = dto.BranchCode,
                CityCode = dto.CityCode,
                CreationDate = ToDateOnly(dto.CreationDate),
                WithdrawalDate = ToDateOnly(dto.WithdrawalDate),
                Cas4uCode = dto.Cas4uCode,
                FundCurrency = dto.FundCurrency,
                FundType = dto.FundType,
                FundStatus = dto.FundStatus
            };

            _db.AdmFondos.Add(entity);
            await _db.SaveChangesAsync();

            return ServiceResult.SuccessResult("Fondo creado exitosamente.");
        }

        public async Task<ServiceResult> UpdateAsync(FundUpsertDto dto, string userId)
        {
            var errors = ValidateDto(dto, isCreate: false);
            if (errors is { Count: > 0 })
                return ServiceResult.FailureResult("Validación", errors: ToErrors(errors));

            Normalize(dto);

            var entity = await _db.AdmFondos.FirstOrDefaultAsync(f => f.FundCode == dto.FundCode);
            if (entity is null)
                return ServiceResult.FailureResult("No se encontró el fondo solicitado.", code: "NoEncontrado");

            if (!_branchCtx.AllBranches && entity.BranchCode.HasValue && _branchCtx.CurrentBranchId.HasValue)
            {
                if (entity.BranchCode.Value != _branchCtx.CurrentBranchId.Value)
                    return ServiceResult.FailureResult("No se encontró.", errors: ToErrors("No se encontró el permiso."));
            }

            var hadChanges = false;

            hadChanges |= SetProp(entity.VatcoFundCode, dto.VatcoFundCode, v => entity.VatcoFundCode = v);
            hadChanges |= SetProp(entity.ClientCode, dto.ClientCode, v => entity.ClientCode = v);
            hadChanges |= SetProp(entity.FundName, dto.FundName, v => entity.FundName = v);
            hadChanges |= SetProp(entity.BranchCode, dto.BranchCode, v => entity.BranchCode = v);
            hadChanges |= SetProp(entity.CityCode, dto.CityCode, v => entity.CityCode = v);

            var newCreation = ToDateOnly(dto.CreationDate);
            var newWithdrawal = ToDateOnly(dto.WithdrawalDate);

            hadChanges |= SetProp(entity.CreationDate, newCreation, v => entity.CreationDate = v);
            hadChanges |= SetProp(entity.WithdrawalDate, newWithdrawal, v => entity.WithdrawalDate = v);

            hadChanges |= SetProp(entity.Cas4uCode, dto.Cas4uCode, v => entity.Cas4uCode = v);
            hadChanges |= SetProp(entity.FundCurrency, dto.FundCurrency, v => entity.FundCurrency = v);
            hadChanges |= SetProp(entity.FundType, dto.FundType, v => entity.FundType = v);
            hadChanges |= SetProp(entity.FundStatus, dto.FundStatus, v => entity.FundStatus = v);

            if (!hadChanges)
                return ServiceResult.SuccessResult("No se realizaron cambios.");

            await _db.SaveChangesAsync();
            return ServiceResult.SuccessResult("Fondo actualizado exitosamente.");
        }

        public async Task<ServiceResult> ChangeStatusAsync(string fundCode, bool newStatus, string userId)
        {
            if (string.IsNullOrWhiteSpace(fundCode))
                return ServiceResult.FailureResult("Fondo requerido.", code: "Requerido");

            var entity = await _db.AdmFondos
                .Include(f => f.Client)
                .FirstOrDefaultAsync(f => f.FundCode == fundCode);

            if (entity is null)
                return ServiceResult.FailureResult("No se encontró el fondo solicitado.", code: "NoEncontrado");

            if (!_branchCtx.AllBranches && entity.BranchCode.HasValue && _branchCtx.CurrentBranchId.HasValue)
            {
                if (entity.BranchCode.Value != _branchCtx.CurrentBranchId.Value)
                    return ServiceResult.FailureResult("No se encontró.", errors: ToErrors("No se encontró el permiso."));
            }

            if (entity.FundStatus == newStatus)
                return ServiceResult.SuccessResult("El estado ya estaba establecido.");

            entity.FundStatus = newStatus;
            await _db.SaveChangesAsync();

            return ServiceResult.SuccessResult("Estado actualizado correctamente.");
        }

        // -------------------------
        // Helpers
        // -------------------------

        private static void Normalize(FundUpsertDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.FundCode))
                dto.FundCode = dto.FundCode.Trim();

            if (!string.IsNullOrWhiteSpace(dto.FundName))
                dto.FundName = dto.FundName.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(dto.Cas4uCode))
                dto.Cas4uCode = dto.Cas4uCode.Trim();

            if (!string.IsNullOrWhiteSpace(dto.FundCurrency))
                dto.FundCurrency = dto.FundCurrency.Trim().ToUpperInvariant();
        }

        private static DateOnly? ToDateOnly(DateTime? dt)
            => dt.HasValue ? DateOnly.FromDateTime(dt.Value.Date) : (DateOnly?)null;

        private static List<string> ValidateDto(FundUpsertDto dto, bool isCreate)
        {
            var errs = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FundCode))
                errs.Add("El código del fondo es obligatorio.");

            var creation = dto.CreationDate?.Date;
            var withdrawal = dto.WithdrawalDate?.Date;
            if (creation.HasValue && withdrawal.HasValue && withdrawal.Value < creation.Value)
                errs.Add("La fecha de retiro no puede ser anterior a la de creación.");

            if (dto.FundStatus && string.IsNullOrWhiteSpace(dto.FundName)) errs.Add("Nombre del fondo es obligatorio si el estado es activo.");
            return errs;
        }

        private static Dictionary<string, string[]> ToErrors(params string[] messages)
            => new() { { "General", messages } };

        private static Dictionary<string, string[]> ToErrors(List<string> messages)
            => new() { { "General", messages.ToArray() } };

        private static bool SetProp<T>(T current, T value, Action<T> setter)
        {
            if (!Equals(current, value))
            {
                setter(value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Asigna el nuevo valor si es distinto del actual y devuelve true si hubo cambio.
        /// </summary>
        private static bool Set<T>(ref T target, T value)
        {
            if (!Equals(target, value))
            {
                target = value;
                return true;
            }
            return false;
        }
    }
}
