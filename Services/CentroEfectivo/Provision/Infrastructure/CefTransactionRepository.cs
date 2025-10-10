using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;

namespace VCashApp.Services.CentroEfectivo.Provision.Infrastructure
{
    /// <summary>Repositorio EF para CefTransaction en el contexto de Provisión.</summary>
    public sealed class CefTransactionRepository : ICefTransactionRepository
    {
        private readonly AppDbContext _db;

        public CefTransactionRepository(AppDbContext db) => _db = db;

        public async Task<int> AddProvisionAsync(CefTransactionNewArgs args)
        {
            var tx = new Models.Entities.CefTransaction
            {
                BranchCode = 0, // ajusta si tu UI lo provee
                ServiceOrderId = args.ServiceOrderId,
                RouteId = null,
                SlipNumber = 0,
                Currency = args.Currency,
                TransactionType = "Provision", // etiqueta de negocio
                DeclaredBagCount = 0,
                DeclaredEnvelopeCount = 0,
                DeclaredCheckCount = 0,
                DeclaredDocumentCount = 0,
                DeclaredBillValue = args.DeclaredBill,
                DeclaredCoinValue = args.DeclaredCoin,
                DeclaredDocumentValue = 0,
                TotalDeclaredValue = args.DeclaredBill + args.DeclaredCoin,
                TotalDeclaredValueInWords = null,
                TotalCountedValue = 0,
                TotalCountedValueInWords = null,
                ValueDifference = 0,
                InformativeIncident = args.Observations,
                IsCustody = false,
                IsPointToPoint = false,
                TransactionStatus = "ProvisionEnProceso",
                RegistrationDate = DateTime.Now,
                RegistrationUser = args.UserId,
                CountingStartDate = null,
                CountingEndDate = null,
                LastUpdateDate = DateTime.Now,
                LastUpdateUser = args.UserId,
                RegistrationIP = null
            };

            _db.CefTransactions.Add(tx);
            await _db.SaveChangesAsync();
            return tx.Id;
        }

        public async Task<dynamic?> GetAsync(int txId)
        {
            return await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == txId);
        }

        public async Task<dynamic?> GetWithTotalsAsync(int txId)
        {
            return await _db.CefTransactions
                .Include(t => t.Containers)
                    .ThenInclude(c => c.ValueDetails)
                .FirstOrDefaultAsync(t => t.Id == txId);
        }

        public Task UpdateAsync(dynamic entity)
        {
            _db.CefTransactions.Update((Models.Entities.CefTransaction)entity);
            return Task.CompletedTask;
        }

        /// <summary>Recalcula totales del encabezado a partir de detalles.</summary>
        public async Task RecalculateTotalsAsync(int txId)
        {
            var tx = await _db.CefTransactions
                .Include(t => t.Containers)
                    .ThenInclude(c => c.ValueDetails)
                .FirstOrDefaultAsync(t => t.Id == txId) ?? throw new InvalidOperationException("Tx no existe");

            var counted = tx.Containers
                .SelectMany(c => c.ValueDetails ?? Enumerable.Empty<Models.Entities.CefValueDetail>())
                .Sum(v => v.CalculatedAmount);

            tx.TotalCountedValue = counted ?? 0;
            tx.ValueDifference = (tx.TotalCountedValue) - (tx.TotalDeclaredValue);
            tx.LastUpdateDate = DateTime.Now;

            // Derivados (si los usas)
            tx.DeclaredEnvelopeCount = tx.Containers.Count(c => c.ContainerType == "Sobre");
            tx.DeclaredBagCount = tx.Containers.Count(c => c.ContainerType != "Sobre");
        }
    }
}