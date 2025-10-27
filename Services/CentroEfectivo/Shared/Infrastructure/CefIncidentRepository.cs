using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Services.CentroEfectivo.Shared.Domain;
using VCashApp.Services.CentroEfectivo.Shared.Incidents;

namespace VCashApp.Services.CentroEfectivo.Shared.Infrastructure
{
    public sealed class CefIncidentRepository : ICefIncidentRepository
    {
        private readonly AppDbContext _db;
        public CefIncidentRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CefIncident> RegisterAsync(CefIncidentViewModel vm, string reportedUserId)
        {
            if (!vm.CefTransactionId.HasValue && !vm.CefContainerId.HasValue && !vm.CefValueDetailId.HasValue)
                throw new InvalidOperationException("Una novedad debe estar asociada al menos a una transacción, contenedor o detalle.");

            var desiredCode = IncidentTypeCode.ToMasterCode(vm.IncidentType);
            var tipo = await _db.CefIncidentTypes
                .FirstOrDefaultAsync(it => it.Code == desiredCode || it.Code == vm.IncidentType.ToString())
                ?? throw new InvalidOperationException($"El tipo de novedad '{vm.IncidentType}' no está configurado.");

            decimal affectedAmount = vm.AffectedAmount;
            if (affectedAmount <= 0m && vm.AffectedDenomination.HasValue && vm.AffectedQuantity.HasValue)
            {
                var face = await _db.AdmDenominaciones
                    .Where(d => d.CodDenominacion == vm.AffectedDenomination.Value)
                    .Select(d => d.ValorDenominacion)
                    .FirstOrDefaultAsync() ?? 0m;
                affectedAmount = face * vm.AffectedQuantity.Value;
            }

            var inc = new CefIncident
            {
                CefTransactionId = vm.CefTransactionId,
                CefContainerId = vm.CefContainerId,
                CefValueDetailId = vm.CefValueDetailId,
                IncidentTypeId = tipo.Id,
                AffectedAmount = affectedAmount,
                AffectedDenomination = vm.AffectedDenomination,
                AffectedQuantity = vm.AffectedQuantity,
                Description = vm.Description,
                ReportedUserId = reportedUserId,
                IncidentDate = DateTime.Now,
                IncidentStatus = "Reportada"
            };

            if (!vm.CefTransactionId.HasValue && vm.CefContainerId.HasValue)
            {
                var parent = await _db.CefContainers.AsNoTracking()
                                   .FirstOrDefaultAsync(c => c.Id == vm.CefContainerId.Value);
                inc.CefTransactionId = parent?.CefTransactionId;
            }

            await _db.CefIncidents.AddAsync(inc);
            await _db.SaveChangesAsync();
            return inc;
        }

        public async Task<bool> ResolveAsync(int incidentId, string newStatus)
        {
            var inc = await _db.CefIncidents.Include(i => i.IncidentType)
                .FirstOrDefaultAsync(i => i.Id == incidentId);

            if (inc != null) return false;

            if (newStatus != "Ajustada" && newStatus != "Closed")
                throw new InvalidOperationException($"Estado '{newStatus}' inválido. Use 'Ajustada' o 'Closed'.");

            if (inc?.IncidentStatus != "Reportada" && inc?.IncidentStatus != "UnderReview")
                return false;

            inc.IncidentStatus = newStatus;
            _db.CefIncidents.Update(inc);
            await _db.SaveChangesAsync();

            if (inc.CefTransactionId.HasValue)
            {
                var tx = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == inc.CefTransactionId.Value);
                if (tx != null)
                {
                    var effects = await SumApprovedEffectByTransactionAsync(tx.Id);
                    tx.ValueDifference = (tx.TotalCountedValue - tx.TotalDeclaredValue) + effects;
                    _db.CefTransactions.Update(tx);
                    await _db.SaveChangesAsync();
                }
            }
            return true;
        }

        public async Task<List<CefIncident>> GetAsync(int? txId = null, int? containerId = null, int? valueDetailId = null)
        {
            IQueryable<CefIncident> q = _db.CefIncidents.Include(i => i.IncidentType);
            if (txId.HasValue) q = q.Where(i => i.CefTransactionId == txId.Value);
            if (containerId.HasValue) q = q.Where(i => i.CefContainerId == containerId.Value);
            if (valueDetailId.HasValue) q = q.Where(i => i.CefValueDetailId == valueDetailId.Value);
            return await q.OrderByDescending(i => i.IncidentDate).ToListAsync();
        }

        public async Task<CefIncident?> GetByIdAsync(int incidentId)
        {
            return await _db.CefIncidents.Include(i => i.IncidentType)
                .FirstOrDefaultAsync(i => i.Id == incidentId);
        }

        public async Task<List<CefIncidentType>> GetTypesAsync()
        {
            return await _db.CefIncidentTypes.AsNoTracking().OrderBy(t => t.Code).ToListAsync();
        }

        public async Task<decimal> SumApprovedEffectByTransactionAsync(int txId)
        {
            var incidents = await _db.CefIncidents
                .Include(i => i.IncidentType)
                .Where(i => i.CefTransactionId == txId && i.IncidentStatus == "Ajustada")
                .ToListAsync();

            var denomIds = incidents.Where(i => i.AffectedDenomination.HasValue)
                                    .Select(i => (int)i.AffectedDenomination!.Value)
                                    .Distinct().ToList();

            var denomMap = await _db.AdmDenominaciones
                .Where(d => denomIds.Contains(d.CodDenominacion))
                .Select(d => new { d.CodDenominacion, d.ValorDenominacion })
                .ToDictionaryAsync(x => x.CodDenominacion, x => (decimal)x.ValorDenominacion);

            decimal sum = 0m;
            foreach (var inc in incidents)
            {
                if (!IncidentTypeCode.TryFromCode(inc.IncidentType?.Code, out var cat)) continue;
                var sign = cat.EffectSign();
                decimal amount;
                if (inc.AffectedAmount != 0) amount = inc.AffectedAmount;
                else
                {
                    var face = 0m;
                    if (inc.AffectedDenomination.HasValue && denomMap.TryGetValue((int)inc.AffectedDenomination.Value, out var fv))
                        face = fv;
                    var qty = inc.AffectedQuantity ?? 0;
                    amount = face * qty;
                }
                sum += sign * amount;
            }
            return sum;
        }

        public async Task<decimal> SumApprovedEffectByContainerAsync(int containerId)
        {
            var incidents = await _db.CefIncidents
                .Include(i => i.IncidentType)
                .Where(i => i.CefContainerId == containerId && i.IncidentStatus == "Ajustada")
                .ToListAsync();

            var denomIds = incidents.Where(i => i.AffectedDenomination.HasValue)
                                    .Select(i => (int)i.AffectedDenomination!.Value)
                                    .Distinct().ToList();

            var denomMap = await _db.AdmDenominaciones
                .Where(d => denomIds.Contains(d.CodDenominacion))
                .Select(d => new { d.CodDenominacion, d.ValorDenominacion })
                .ToDictionaryAsync(x => x.CodDenominacion, x => (decimal)x.ValorDenominacion);

            decimal sum = 0m;
            foreach (var inc in incidents)
            {
                if (!IncidentTypeCode.TryFromCode(inc.IncidentType?.Code, out var cat)) continue;
                var sign = cat.EffectSign();
                decimal amount;
                if (inc.AffectedAmount != 0) amount = inc.AffectedAmount;
                else
                {
                    var face = 0m;
                    if (inc.AffectedDenomination.HasValue && denomMap.TryGetValue((int)inc.AffectedDenomination.Value, out var fv))
                        face = fv;
                    var qty = inc.AffectedQuantity ?? 0;
                    amount = face * qty;
                }
                sum += sign * amount;
            }
            return sum;
        }

        public async Task<bool> HasPendingByTransactionAsync(int txId)
        {
            return await _db.CefIncidents.Include(i => i.CefContainer)
                .AnyAsync(i =>
                    (i.CefTransactionId == txId ||
                     (i.CefContainer != null && i.CefContainer.CefTransactionId == txId))
                    && (i.IncidentStatus == "Reportada" || i.IncidentStatus == "UnderReview"));
        }

        public async Task<bool> UpdateReportedAsync(int id, int? newTypeId, CefIncidentTypeCategoryEnum? newType,
    int? newDenominationId, int? newQuantity, decimal? newAmount, string? newDescription)
        {
            var inc = await _db.CefIncidents.Include(i => i.IncidentType).FirstOrDefaultAsync(i => i.Id == id);
            if (inc == null) return false;

            if (newTypeId.HasValue)
            {
                var tipo = await _db.CefIncidentTypes.FirstOrDefaultAsync(t => t.Id == newTypeId.Value);
                if (tipo == null) throw new InvalidOperationException($"Tipo de novedad Id={newTypeId} no existe.");
                inc.IncidentTypeId = tipo.Id;
            }
            else if (newType.HasValue)
            {
                var code = IncidentTypeCode.ToMasterCode(newType.Value);
                var tipo = await _db.CefIncidentTypes.FirstOrDefaultAsync(it => it.Code == code || it.Code == newType.Value.ToString());
                if (tipo == null)
                    throw new InvalidOperationException($"El tipo de novedad '{newType}' no está configurado (falta '{code}' en la tabla maestra).");
                inc.IncidentTypeId = tipo.Id;
            }

            if (newDenominationId.HasValue) inc.AffectedDenomination = newDenominationId.Value;
            if (newQuantity.HasValue) inc.AffectedQuantity = newQuantity.Value;
            if (newAmount.HasValue) inc.AffectedAmount = newAmount.Value;
            if (!string.IsNullOrWhiteSpace(newDescription)) inc.Description = newDescription.Trim();

            _db.CefIncidents.Update(inc);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReportedAsync(int id)
        {
            var inc = await _db.CefIncidents.FirstOrDefaultAsync(i => i.Id == id);
            if (inc == null) return false;
            _db.CefIncidents.Remove(inc);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
