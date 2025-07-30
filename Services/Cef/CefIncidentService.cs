using VCashApp.Services;
using VCashApp.Data;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace VCashApp.Services.Cef
{
    /// <summary>
    /// Implementación del servicio para la gestión de Novedades de Centro de Efectivo.
    /// </summary>
    public class CefIncidentService : ICefIncidentService
    {
        private readonly AppDbContext _context;

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

            var tipoNovedadEntity = await _context.CefIncidentTypes.FirstOrDefaultAsync(it => it.Code == incidentViewModel.IncidentType.ToString());
            if (tipoNovedadEntity == null)
            {
                throw new InvalidOperationException($"El tipo de novedad '{incidentViewModel.IncidentType}' no es válido o no está configurado en la tabla Adm_TiposNovedad.");
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

            await _context.CefIncidents.AddAsync(incident);
            await _context.SaveChangesAsync();
            return incident;
        }

        /// <inheritdoc/>
        public async Task<bool> ResolveIncidentAsync(int incidentId, string newStatus)
        {
            var incident = await _context.CefIncidents.FirstOrDefaultAsync(i => i.Id == incidentId);
            if (incident == null) return false;

            if (newStatus != "Adjusted" && newStatus != "Closed")
            {
                throw new InvalidOperationException($"El estado '{newStatus}' no es un estado de resolución válido para la novedad.");
            }

            if (incident.IncidentStatus == "Reported" || incident.IncidentStatus == "UnderReview")
            {
                incident.IncidentStatus = newStatus;
                _context.CefIncidents.Update(incident);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
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
    }
}