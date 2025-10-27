using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Services.CentroEfectivo.Collection.Domain;
using VCashApp.Services.CentroEfectivo.Provision.Domain;
using VCashApp.Services.CentroEfectivo.Shared.Domain;
using VCashApp.Utils;

namespace VCashApp.Services.CentroEfectivo.Collection.Application
{
    /// <summary>
    /// Orquesta reglas de negocio para la Provisión. No conoce EF ni UI.
    /// </summary>
    public sealed class CollectionService : ICollectionService
    {
        private readonly AppDbContext _db;
        private readonly IAuditLogger _audit;
        private readonly ICollectionStateMachine _sm;
        private readonly ICountingPolicy _policy;
        private readonly ICefContainerRepository _containers;
        private readonly ICefIncidentRepository _incidents;
        private readonly IAllowedValueTypesPolicy _allowed;

        public CollectionService(
            AppDbContext db,
            IAuditLogger audit,
            ICollectionStateMachine sm,
            ICountingPolicy policy,
            ICefContainerRepository containers,
            ICefIncidentRepository incidents,
            IAllowedValueTypesPolicy allowed
        )
        {
            _db = db;
            _audit = audit;
            _sm = sm;
            _policy = policy;
            _containers = containers;
            _incidents = incidents;
            _allowed = allowed;
        }

        public async Task<int> CreateCollectionAsync(CreateCollectionCmd cmd, string userId)
        {
            if (!_policy.CanCreate(cmd.ServiceOrderId, cmd.Currency, cmd.DeclaredTotalValue))
                throw new InvalidOperationException("Datos inválidos para crear transacción.");

            var service = await _db.CgsServicios.FirstOrDefaultAsync(s => s.ServiceOrderId == cmd.ServiceOrderId)
                          ?? throw new InvalidOperationException($"La Orden de Servicio '{cmd.ServiceOrderId}' no existe.");

            var tx = await _db.CefTransactions.FirstOrDefaultAsync(t => t.ServiceOrderId == cmd.ServiceOrderId);
            if (tx == null)
                throw new InvalidOperationException($"No existe una transacción CEF para la Orden de Servicio '{cmd.ServiceOrderId}'. Debe haberse generado al crear el servicio.");


            tx.SlipNumber = cmd.SlipNumber ?? 0;
            tx.Currency = cmd.Currency;
            tx.DeclaredDocumentValue = 0m;
            tx.DeclaredBagCount = cmd.DeclaredBagCount;
            tx.TotalDeclaredValue = cmd.DeclaredTotalValue;
            tx.TotalDeclaredValueInWords = AmountInWordsHelper.ToSpanishCurrency(tx.TotalDeclaredValue, cmd.Currency);
            tx.InformativeIncident = cmd.Observations;
            tx.TransactionType = tx.TransactionType;
            _sm.EnsureCanMove(tx.TransactionStatus, CefTransactionStatusEnum.EncoladoParaConteo.ToString(), tx.Id);
            tx.TransactionStatus = CefTransactionStatusEnum.EncoladoParaConteo.ToString();

            await _db.SaveChangesAsync();

            _audit.Info("CEF.Checkin", $"Check-in OS {tx.ServiceOrderId}, Planilla {tx.SlipNumber}.", "EncoladoParaConteo", "CefTransaction", tx.Id.ToString(), tx.ServiceOrderId);

            if (service.StatusCode < 1)
            {
                service.StatusCode = 1;
                _db.CgsServicios.Update(service);
                await _db.SaveChangesAsync();
            }

            return tx.Id;
        }

        public async Task SaveContainersAsync(int txId, SaveCollectionContainersCmd cmd, string userId)
        {
            await _containers.SaveContainersAndDetailsAsync(
                txId,
                cmd.Containers,
                userId,
                _allowed
            );

            var transaction = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == txId)
                              ?? throw new InvalidOperationException($"Transacción {txId} no existe.");

            if (transaction.TransactionStatus == CefTransactionStatusEnum.EncoladoParaConteo.ToString()
                && cmd.Containers.Any(c => (c.ValueDetails?.Count ?? 0) > 0))
            {
                _sm.EnsureCanMove(transaction.TransactionStatus, CefTransactionStatusEnum.Conteo.ToString(), transaction.Id);
                transaction.TransactionStatus = CefTransactionStatusEnum.Conteo.ToString();
                _db.CefTransactions.Update(transaction);
                await _db.SaveChangesAsync();

                var service = await _db.CgsServicios.FirstOrDefaultAsync(s => s.ServiceOrderId == transaction.ServiceOrderId);
                if (service != null && service.StatusCode != 4)
                {
                    service.StatusCode = 4;
                    _db.CgsServicios.Update(service);
                    await _db.SaveChangesAsync();
                }

                _audit.Info("CEF.Counting", $"Tx {transaction.Id} movida a CONTEO.", "Conteo", "CefTransaction", transaction.Id.ToString(), transaction.ServiceOrderId);
            }

            await RecalcTotalsAndNetDiffAsync(txId);
        }

        public async Task FinalizeAsync(int txId, string userId)
        {
            var tx = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == txId)
                     ?? throw new InvalidOperationException($"Transacción {txId} no existe.");

            if (!_policy.CanFinalize(tx))
                throw new InvalidOperationException("No cumple condiciones para finalizar (pasar a revisión).");

            _sm.EnsureCanMove(tx.TransactionStatus, CefTransactionStatusEnum.PendienteRevision.ToString(), tx.Id);
            tx.TransactionStatus = CefTransactionStatusEnum.PendienteRevision.ToString();
            _db.CefTransactions.Update(tx);
            await _db.SaveChangesAsync();

            await CollectionStateMachine.SyncServiceIfAdvanceAsync(_db, tx.ServiceOrderId!, CefTransactionStatusEnum.PendienteRevision);
        }

        public async Task ApproveAsync(int txId, string reviewerUserId)
        {
            var tx = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == txId)
                     ?? throw new InvalidOperationException($"Transacción {txId} no existe.");

            if (!_policy.CanApprove(tx))
                throw new InvalidOperationException("La transacción no está lista para aprobar.");

            _sm.EnsureCanMove(tx.TransactionStatus, CefTransactionStatusEnum.Aprobado.ToString(), tx.Id);
            tx.TransactionStatus = CefTransactionStatusEnum.Aprobado.ToString();
            tx.ReviewerUserId = reviewerUserId;
            tx.CountingEndDate = DateTime.Now;

            _db.CefTransactions.Update(tx);
            await _db.SaveChangesAsync();

            await CollectionStateMachine.SyncServiceIfAdvanceAsync(_db, tx.ServiceOrderId!, CefTransactionStatusEnum.Aprobado);

            _audit.Info("CEF.Review", $"Transacción {tx.Id} revisada → Aprobado.", "Aprobado", "CefTransaction", tx.Id.ToString(), tx.ServiceOrderId);
        }

        public async Task SaveHeaderAsync(int txId, string? observations, string userId)
        {
            var tx = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == txId)
                     ?? throw new InvalidOperationException($"Transacción {txId} no existe.");
            tx.InformativeIncident = observations;
            tx.LastUpdateDate = DateTime.Now;
            tx.LastUpdateUser = userId;
            _db.CefTransactions.Update(tx);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> UpdateTransactionStatusAsync(int transactionId, CefTransactionStatusEnum newStatus, string reviewerUserId)
        {
            var transaction = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == transactionId);
            if (transaction == null) return false;

            if (transaction.TransactionStatus is nameof(CefTransactionStatusEnum.Aprobado) or nameof(CefTransactionStatusEnum.Rechazado) or nameof(CefTransactionStatusEnum.Cancelado))
                throw new InvalidOperationException($"La transacción {transactionId} ya está en un estado final y no puede ser modificada.");

            _sm.EnsureCanMove(transaction.TransactionStatus, newStatus.ToString(), transaction.Id);

            transaction.TransactionStatus = newStatus.ToString();
            transaction.LastUpdateDate = DateTime.Now;
            transaction.LastUpdateUser = reviewerUserId;

            if (newStatus is CefTransactionStatusEnum.Aprobado or CefTransactionStatusEnum.Rechazado)
                transaction.CountingEndDate = DateTime.Now;

            _db.CefTransactions.Update(transaction);
            await _db.SaveChangesAsync();

            await CollectionStateMachine.SyncServiceIfAdvanceAsync(_db, transaction.ServiceOrderId!, newStatus);
            return true;
        }

        public async Task<bool> ProcessReviewApprovalAsync(CefTransactionReviewViewModel viewModel, string reviewerUserId)
        {
            var transaction = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == viewModel.Id);
            if (transaction == null) return false;

            if (transaction.TransactionStatus != CefTransactionStatusEnum.PendienteRevision.ToString())
                throw new InvalidOperationException($"La transacción {transaction.Id} no está en estado 'Pendiente de Revisión'.");

            var typeCode = (viewModel.TransactionTypeCode ?? transaction.TransactionType)?.Trim().ToUpperInvariant();
            var isProvision = typeCode == "PV" || typeCode == "PR";

            var nextStatus = viewModel.NewStatus;
            if (isProvision && viewModel.NewStatus == CefTransactionStatusEnum.Aprobado)
                nextStatus = CefTransactionStatusEnum.ListoParaEntrega;

            if (isProvision)
                _ = nextStatus;
            else
                _sm.EnsureCanMove(transaction.TransactionStatus, nextStatus.ToString(), transaction.Id);

            transaction.TransactionStatus = nextStatus.ToString();
            transaction.InformativeIncident = viewModel.FinalObservations;
            transaction.ReviewerUserId = reviewerUserId;
            transaction.CountingEndDate = DateTime.Now;

            _db.CefTransactions.Update(transaction);
            await _db.SaveChangesAsync();

            await CollectionStateMachine.SyncServiceIfAdvanceAsync(_db, transaction.ServiceOrderId!, nextStatus);

            return true;
        }

        public async Task<bool> RecalcTotalsAndNetDiffAsync(int txId)
        {
            var tx = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == txId);
            if (tx == null) return false;

            var counted = await _containers.SumCountedAsync(txId);
            var effect = await _incidents.SumApprovedEffectByContainerAsync(txId);

            tx.TotalCountedValue = counted;
            tx.ValueDifference = (tx.TotalCountedValue - tx.TotalDeclaredValue) + effect;

            _db.CefTransactions.Update(tx);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}