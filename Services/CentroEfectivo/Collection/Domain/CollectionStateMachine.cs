using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;

namespace VCashApp.Services.CentroEfectivo.Collection.Domain
{
    /// <summary>Reglas de transición para Recolección + mapeo a estados de Servicio (0..7).</summary>
    public sealed class CollectionStateMachine : ICollectionStateMachine
    {
        private static readonly IReadOnlyDictionary<CefTransactionStatusEnum, CefTransactionStatusEnum[]> AllowedNext =
            new Dictionary<CefTransactionStatusEnum, CefTransactionStatusEnum[]>
            {
                [CefTransactionStatusEnum.RegistroTesoreria] = new[] { CefTransactionStatusEnum.EncoladoParaConteo, CefTransactionStatusEnum.Cancelado },
                [CefTransactionStatusEnum.EncoladoParaConteo] = new[] { CefTransactionStatusEnum.Conteo, CefTransactionStatusEnum.Cancelado },
                [CefTransactionStatusEnum.Conteo] = new[] { CefTransactionStatusEnum.PendienteRevision, CefTransactionStatusEnum.Cancelado },
                [CefTransactionStatusEnum.PendienteRevision] = new[] { CefTransactionStatusEnum.Aprobado, CefTransactionStatusEnum.Rechazado, CefTransactionStatusEnum.Cancelado },
                [CefTransactionStatusEnum.Aprobado] = Array.Empty<CefTransactionStatusEnum>(),
                [CefTransactionStatusEnum.Rechazado] = Array.Empty<CefTransactionStatusEnum>(),
                [CefTransactionStatusEnum.Cancelado] = Array.Empty<CefTransactionStatusEnum>(),
            };
        public void EnsureCanMove(string current, string next, int txId)
        {
            if (!Enum.TryParse(current, out CefTransactionStatusEnum cur))
                throw new InvalidOperationException($"Estado actual inválido: '{current}'.");

            if (!Enum.TryParse(next, out CefTransactionStatusEnum target))
                throw new InvalidOperationException($"Estado destino inválido: '{next}'.");

            if (!AllowedNext.TryGetValue(cur, out var nexts) || !nexts.Contains(target))
            {
                var allowed = AllowedNext.TryGetValue(cur, out var arr) ? string.Join(", ", arr) : "—";
                throw new InvalidOperationException($"Transición no permitida (Tx {txId}): {cur} → {target}. Permitidas: {allowed}.");
            }
        }

        public static int? MapServiceStatus(CefTransactionStatusEnum tx) => tx switch
        {
            CefTransactionStatusEnum.RegistroTesoreria => 0, // Solicitado
            CefTransactionStatusEnum.EncoladoParaConteo => 1, // Confirmado
            CefTransactionStatusEnum.Conteo => 4, // Atención
            CefTransactionStatusEnum.PendienteRevision => 4, // Atención
            CefTransactionStatusEnum.Aprobado => 5, // Finalizado
            CefTransactionStatusEnum.Rechazado => 2, // Rechazado
            CefTransactionStatusEnum.Cancelado => 6, // Cancelado
            _ => null
        };

        /// <summary>Sincroniza estado CGS si avanza (no retrocede salvo terminales 2/5/6).</summary>
        public static async Task SyncServiceIfAdvanceAsync(AppDbContext ctx, string serviceOrderId, CefTransactionStatusEnum target)
        {
            var srv = await ctx.CgsServicios.FirstOrDefaultAsync(s => s.ServiceOrderId == serviceOrderId);
            if (srv == null) return;

            var to = MapServiceStatus(target);
            if (!to.HasValue) return;

            var current = srv.StatusCode;
            var terminal = to.Value is 2 or 5 or 6;
            var shouldAdvance = terminal || to.Value > current;

            if (!shouldAdvance || current == to.Value) return;

            srv.StatusCode = to.Value;
            ctx.CgsServicios.Update(srv);
            await ctx.SaveChangesAsync();
        }
    }
}
