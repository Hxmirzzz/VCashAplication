using System;
using VCashApp.Enums;

namespace VCashApp.Services.CentroEfectivo.Provision.Domain
{
    /// <summary>Transiciones válidas para el ciclo de provisión.</summary>
    public sealed class ProvisionStateMachine : IProvisionStateMachine
    {
        public void EnsureCanMove(string current, string next, int txId)
        {
            if (current == nameof(CefTransactionStatusEnum.ProvisionEnProceso)
                && next == nameof(CefTransactionStatusEnum.ListoParaEntrega)) return;

            if (current == nameof(CefTransactionStatusEnum.ListoParaEntrega)
                && next == nameof(CefTransactionStatusEnum.Entregado)) return;

            throw new InvalidOperationException($"Transición inválida {current} → {next} (tx {txId}).");
        }
    }
}