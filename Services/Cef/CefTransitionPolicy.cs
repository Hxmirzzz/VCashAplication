using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;

namespace VCashApp.Services.Cef
{
    /// <summary>
    /// Reglas de transición para Recolección + mapeo a estados de Servicio (0..7).
    /// </summary>
    public static class CefTransitionPolicy
    {
        // Transiciones permitidas (Recolección)
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

        /// <summary>
        /// Lanza InvalidOperationException si la transición no es válida para Recolección.
        /// </summary>
        public static void EnsureAllowedRecoleccion(string currentStatusString, CefTransactionStatusEnum target, int? txIdForMessage = null)
        {
            if (!Enum.TryParse<CefTransactionStatusEnum>(currentStatusString, out var current))
                throw new InvalidOperationException($"Estado actual inválido: '{currentStatusString}'.");

            if (!AllowedNext.TryGetValue(current, out var nexts) || !nexts.Contains(target))
            {
                var idTxt = txIdForMessage.HasValue ? $" (Tx {txIdForMessage})" : string.Empty;
                var allowed = AllowedNext.TryGetValue(current, out var arr) ? string.Join(", ", arr) : "—";
                throw new InvalidOperationException($"Transición no permitida{idTxt}: {current} → {target}. Permitidas: {allowed}.");
            }
        }

        /// <summary>
        /// Mapea estado de transacción (Recolección) a CodEstado del Servicio (0..7).
        /// </summary>
        public static int? MapServiceStatusRecoleccion(CefTransactionStatusEnum tx)
        {
            return tx switch
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
        }

        /// <summary>
        /// Sube el estado del Servicio si corresponde (no retrocede, salvo terminales).
        /// Llama sólo si QUIERES que esta clase sincronice; si ya lo haces inline, no la llames.
        /// </summary>
        public static async Task SyncServiceIfAdvanceAsync(AppDbContext ctx, string serviceOrderId, CefTransactionStatusEnum target)
        {
            var srv = await ctx.CgsServicios.FirstOrDefaultAsync(s => s.ServiceOrderId == serviceOrderId);
            if (srv == null) return;

            var to = MapServiceStatusRecoleccion(target);
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