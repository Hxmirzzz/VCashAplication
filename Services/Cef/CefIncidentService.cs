using DocumentFormat.OpenXml.Math;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;

namespace VCashApp.Services.Cef
{
    /// <summary>
    /// Implementación del servicio para la gestión de Novedades de Centro de Efectivo.
    /// </summary>
    public class CefIncidentService : ICefIncidentService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CefIncidentService"/> class.
        /// </summary>
        /// <param name="context">The database context used to interact with the application's data store.  This parameter cannot be <see
        /// langword="null"/>.</param>
        public CefIncidentService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<CefIncident> RegisterIncidentAsync(CefIncidentViewModel incidentViewModel, string reportedUserId)
        {
            if (!incidentViewModel.CefTransactionId.HasValue && !incidentViewModel.CefContainerId.HasValue && !incidentViewModel.CefValueDetailId.HasValue)
            {
                throw new InvalidOperationException("Una novedad debe estar asociada al menos a una transacción, un contenedor o un detalle de valor.");
            }

            var desiredCode = IncidentTypeCodeMap.ToMasterCode(incidentViewModel.IncidentType);

            var tipoNovedadEntity = await _context.CefIncidentTypes
                .FirstOrDefaultAsync(it =>
                    it.Code == desiredCode ||
                    it.Code == incidentViewModel.IncidentType.ToString()
                );

            if (tipoNovedadEntity == null)
            {
                throw new InvalidOperationException(
                    $"El tipo de novedad '{incidentViewModel.IncidentType}' no está configurado. " +
                    $"Falta el código '{desiredCode}' en la tabla maestra."
                );
            }

            var incident = new CefIncident
            {
                CefTransactionId = incidentViewModel.CefTransactionId,
                CefContainerId = incidentViewModel.CefContainerId,
                CefValueDetailId = incidentViewModel.CefValueDetailId,
                IncidentTypeId = tipoNovedadEntity.Id,
                AffectedAmount = incidentViewModel.AffectedAmount,
                AffectedDenomination = incidentViewModel.AffectedDenomination,
                AffectedQuantity = incidentViewModel.AffectedQuantity,
                Description = incidentViewModel.Description,
                ReportedUserId = reportedUserId,
                IncidentDate = DateTime.Now,
                IncidentStatus = "Reported"
            };

            if (!incidentViewModel.CefTransactionId.HasValue && incidentViewModel.CefContainerId.HasValue)
            {
                var parent = await _context.CefContainers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == incidentViewModel.CefContainerId.Value);
                incident.CefTransactionId = parent?.CefTransactionId;
            }

            await _context.CefIncidents.AddAsync(incident);
            await _context.SaveChangesAsync();
            return incident;
        }

        /// <inheritdoc/>
        public async Task<bool> ResolveIncidentAsync(int incidentId, string newStatus)
        {
            var inc = await _context.CefIncidents
                .Include(i => i.IncidentType)
                .FirstOrDefaultAsync(i => i.Id == incidentId);

            if (inc == null) return false;

            if (newStatus != "Adjusted" && newStatus != "Closed")
                throw new InvalidOperationException($"El estado '{newStatus}' no es válido. Use 'Adjusted' o 'Closed'.");

            if (inc.IncidentStatus != "Reported" && inc.IncidentStatus != "UnderReview")
                return false;

            inc.IncidentStatus = newStatus;
            _context.CefIncidents.Update(inc);
            await _context.SaveChangesAsync();

            if (inc.CefTransactionId.HasValue)
            {
                var tx = await _context.CefTransactions
                    .FirstOrDefaultAsync(t => t.Id == inc.CefTransactionId.Value);

                if (tx != null)
                {
                    var effects = await SumApprovedEffectByTransactionAsync(tx.Id);
                    var counted = tx.TotalCountedValue;
                    var declared = tx.TotalDeclaredValue;

                    tx.ValueDifference = (counted + effects) - declared;

                    _context.CefTransactions.Update(tx);
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<List<CefIncident>> GetIncidentsAsync(int? transactionId = null, int? containerId = null, int? valueDetailId = null)
        {
            IQueryable<CefIncident> query = _context.CefIncidents
                                                    .Include(i => i.IncidentType)
                                                    .AsQueryable();

            if (transactionId.HasValue)
            {
                query = query.Where(i => i.CefTransactionId == transactionId.Value);
            }
            if (containerId.HasValue)
            {
                query = query.Where(i => i.CefContainerId == containerId.Value);
            }
            if (valueDetailId.HasValue)
            {
                query = query.Where(i => i.CefValueDetailId == valueDetailId.Value);
            }

            return await query.OrderByDescending(i => i.IncidentDate).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<CefIncident?> GetIncidentByIdAsync(int incidentId)
        {
            return await _context.CefIncidents
                                 .Include(i => i.IncidentType)
                                 .FirstOrDefaultAsync(i => i.Id == incidentId);
        }

        /// <inheritdoc/>
        public async Task<List<CefIncidentType>> GetAllIncidentTypesAsync()
        {
            return await _context.CefIncidentTypes.ToListAsync();
        }

        public async Task<decimal> SumApprovedEffectByTransactionAsync(int transactionId)
        {
            var incidents = await _context.CefIncidents
                .Include(i => i.IncidentType)
                .Where(i => i.CefTransactionId == transactionId && i.IncidentStatus == "Adjusted")
                .ToListAsync();

            // 1) IDs de denominación usados en las novedades
            var denomIds = incidents
                .Where(i => i.AffectedDenomination.HasValue)
                .Select(i => (int)i.AffectedDenomination!.Value)
                .Distinct()
                .ToList();

            // 2) Carga valores faciales por ID (ajusta el DbSet/campos a tu modelo real)
            var denomMap = await _context.Set<AdmDenominacion>()
                .Where(d => denomIds.Contains(d.CodDenominacion))
                .Select(d => new { d.CodDenominacion, d.ValorDenominacion })
                .ToDictionaryAsync(x => x.CodDenominacion, x => (decimal)x.ValorDenominacion);

            decimal sum = 0m;

            foreach (var inc in incidents)
            {
                if (!IncidentTypeCodeMap.TryFromCode(inc.IncidentType?.Code, out var cat))
                    continue;

                var sign = cat.EffectSign();

                decimal amount;
                if (inc.AffectedAmount != 0)
                {
                    amount = inc.AffectedAmount;
                }
                else
                {
                    var face = 0m;
                    if (inc.AffectedDenomination.HasValue)
                    {
                        var key = (int)inc.AffectedDenomination.Value;
                        if (denomMap.TryGetValue(key, out var faceValue))
                            face = faceValue;
                    }
                    var qty = inc.AffectedQuantity ?? 0;
                    amount = face * qty;
                }

                sum += sign * amount;
            }

            return sum;
        }

        public async Task<bool> HasPendingIncidentsByTransactionAsync(int cefTransactionId)
        {
            return await _context.CefIncidents
                .Include(i => i.CefContainer)
                .AnyAsync(i =>
                    (i.CefTransactionId == cefTransactionId ||
                     (i.CefContainer != null && i.CefContainer.CefTransactionId == cefTransactionId))
                    && (i.IncidentStatus == "Reported" || i.IncidentStatus == "UnderReview"));
        }

        public async Task<decimal> SumApprovedEffectByContainerAsync(int containerId)
        {
            var incidents = await _context.CefIncidents
                .Include(i => i.IncidentType)
                .Where(i => i.CefContainerId == containerId && i.IncidentStatus == "Adjusted")
                .ToListAsync();
            
            var denoms = incidents
                .Where(i => i.AffectedDenomination.HasValue)
                .Select(i => (int)i.AffectedDenomination!.Value)
                .Distinct()
                .ToList();

            var denomMap = await _context.Set<AdmDenominacion>()
                .Where(d => denoms.Contains(d.CodDenominacion))
                .Select(d => new { d.CodDenominacion, d.ValorDenominacion })
                .ToDictionaryAsync(x => x.CodDenominacion, x => (decimal)x.ValorDenominacion);

            decimal sum = 0m;
            foreach (var inc in incidents)
            {
                if (!IncidentTypeCodeMap.TryFromCode(inc.IncidentType?.Code, out var cat))
                    continue;

                var sign = cat.EffectSign();
                decimal amount;
                if (inc.AffectedAmount != 0)
                {
                    amount = inc.AffectedAmount;
                }
                else
                {
                    var face = 0m;
                    if (inc.AffectedDenomination.HasValue)
                    {
                        var key = (int)inc.AffectedDenomination.Value;
                        if (denomMap.TryGetValue(key, out var faceValue))
                            face = faceValue;
                    }
                    var qty = inc.AffectedQuantity ?? 0;
                    amount = face * qty;
                }

                sum += sign * amount;
            }

            return sum;
        }
    }
}