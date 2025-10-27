using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Services.CentroEfectivo.Provision.Domain;
using VCashApp.Services.CentroEfectivo.Shared.Domain;
using LoggingAudit = VCashApp.Services.Logging.IAuditLogger;

namespace VCashApp.Services.CentroEfectivo.Provision.Application
{
    /// <summary>
    /// Orquesta reglas de negocio para la Provisión. No conoce EF ni UI.
    /// </summary>
    public sealed class ProvisionService : IProvisionService
    {
        private readonly ICefTransactionRepository _txRepo;
        private readonly ICefContainerRepository _containerRepo;
        private readonly IProvisionStateMachine _sm;
        private readonly IAllowedValueTypesPolicy _vtPolicy;
        private readonly IEnvelopePolicy _envPolicy;
        private readonly ITolerancePolicy _tolPolicy;
        private readonly IUnitOfWork _uow;
        private readonly LoggingAudit _audit;

        public ProvisionService(
            ICefTransactionRepository txRepo,
            ICefContainerRepository containerRepo,
            IProvisionStateMachine sm,
            IAllowedValueTypesPolicy vtPolicy,
            IEnvelopePolicy envPolicy,
            ITolerancePolicy tolPolicy,
            IUnitOfWork uow,
            LoggingAudit audit)
        {
            _txRepo = txRepo;
            _containerRepo = containerRepo;
            _sm = sm;
            _vtPolicy = vtPolicy;
            _envPolicy = envPolicy;
            _tolPolicy = tolPolicy;
            _uow = uow;
            _audit = audit;
        }

        public async Task<int> CreateProvisionAsync(CreateProvisionCmd cmd, string userId)
        {
            var txId = await _txRepo.AddProvisionAsync(new CefTransactionNewArgs
            {
                ServiceOrderId = cmd.ServiceOrderId,
                Currency = cmd.Currency,
                DeclaredBill = cmd.DeclaredBill,
                DeclaredCoin = cmd.DeclaredCoin,
                Observations = cmd.Observations,
                UserId = userId
            });

            await _uow.SaveChangesAsync();
            _audit.Info("CEF.Provision.Create", "Creación provisión", "OK", "CefTransaction", txId.ToString(), cmd.ServiceOrderId);
            return txId;
        }

        public async Task SaveHeaderAsync(int txId, int slipNumber, string informativeincident, string userId)
        {
            var tx = await _txRepo.GetAsync(txId) ?? throw new InvalidOperationException("Tx no existe.");

            tx.SlipNumber = slipNumber;
            tx.InformativeIncident = informativeincident;
            tx.LastUpdateUser = userId;
            tx.LastUpdateDate = DateTime.Now;

            await _txRepo.UpdateAsync(tx);
            await _uow.SaveChangesAsync();

            _audit.Info("CEF.Provision.SaveHeader", "Cabecera actualizada", "OK", "CefTransaction", txId.ToString(), tx.ServiceOrderId);
        }

        public async Task SaveContainersAsync(int txId, SaveProvisionContainersCmd cmd, string userId)
        {
            var tx = await _txRepo.GetAsync(txId) ?? throw new InvalidOperationException("Tx no existe.");
            if (tx.TransactionStatus != nameof(CefTransactionStatusEnum.ProvisionEnProceso))
                throw new InvalidOperationException("La provisión no está en proceso.");

            foreach (var c in cmd.Containers ?? Array.Empty<CefContainerProcessingViewModel>())
            {
                var isEnv = c.ContainerType == CefContainerTypeEnum.Sobre;
                if (isEnv && !_envPolicy.AllowEnvelopes) throw new InvalidOperationException("Sobres no permitidos en provisión.");
                if (isEnv && c.EnvelopeSubType != CefEnvelopeSubTypeEnum.Efectivo) throw new InvalidOperationException("Sobres solo Efectivo.");

                foreach (var d in c.ValueDetails ?? Enumerable.Empty<CefValueDetailViewModel>())
                {
                    if (!_vtPolicy.IsAllowed(d.ValueType))
                        throw new InvalidOperationException("Solo Billete/Moneda permitidos en provisión.");
                    if (isEnv && !_envPolicy.IsValidEnvelope(c.EnvelopeSubType, d.ValueType))
                        throw new InvalidOperationException("Detalle inválido para sobre de provisión.");
                }
            }

            await _containerRepo.SaveContainersAndDetailsAsync(txId, cmd.Containers, userId, _vtPolicy);
            await _txRepo.RecalculateTotalsAsync(txId);
            await _uow.SaveChangesAsync();

            _audit.Info("CEF.Provision.Save", "Contenedores guardados", "OK", "CefTransaction", txId.ToString(), tx.ServiceOrderId);
        }

        public async Task FinalizeAsync(int txId, string userId)
        {
            var tx = await _txRepo.GetWithTotalsAsync(txId) ?? throw new InvalidOperationException("Tx no existe.");
            if (!_tolPolicy.IsWithinTolerance(tx.TotalDeclaredValue, tx.TotalCountedValue))
                throw new InvalidOperationException("Conteo fuera de tolerancia.");

            _sm.EnsureCanMove(tx.TransactionStatus, nameof(CefTransactionStatusEnum.PendienteRevision), txId);
            tx.TransactionStatus = nameof(CefTransactionStatusEnum.PendienteRevision);
            tx.LastUpdateUser = userId; tx.LastUpdateDate = DateTime.Now;

            await _txRepo.UpdateAsync(tx);
            await _uow.SaveChangesAsync();
            _audit.Info("CEF.Provision.Finalize", "Pendiente de revisión", "OK", "CefTransaction", txId.ToString(), tx.ServiceOrderId);
        }

        public async Task DeliverAsync(int txId, string delivererUserId, string receiverUserId)
        {
            var tx = await _txRepo.GetAsync(txId) ?? throw new InvalidOperationException("Tx no existe.");
            _sm.EnsureCanMove(tx.TransactionStatus, nameof(CefTransactionStatusEnum.Entregado), txId);

            tx.DelivererId = delivererUserId;
            tx.ReceiverId = receiverUserId;
            tx.TransactionStatus = nameof(CefTransactionStatusEnum.Entregado);
            tx.LastUpdateUser = delivererUserId; tx.LastUpdateDate = DateTime.Now;

            await _txRepo.UpdateAsync(tx);
            await _uow.SaveChangesAsync();
            _audit.Info(
                "CEF.Provision.Deliver",
                $"Entregado a ReceiverId={receiverUserId}",
                "OK",
                "CefTransaction",
                txId.ToString(),
                tx.ServiceOrderId
            );
        }
    }
}