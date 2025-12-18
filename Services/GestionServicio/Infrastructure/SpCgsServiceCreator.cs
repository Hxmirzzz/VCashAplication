using DocumentFormat.OpenXml.InkML;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.Servicio;
using VCashApp.Services.DTOs;
using VCashApp.Services.GestionServicio.Domain;
using VCashApp.Utils;

namespace VCashApp.Services.GestionServicio.Infrastructure
{
    public sealed class SpCgsServiceCreator : ICgsServiceCreator
    {
        private readonly AppDbContext _db;
        public SpCgsServiceCreator(AppDbContext db) => _db = db;

        public async Task<ServiceResult> CreateAsync(CgsServiceRequestViewModel vm, string userId, string ip)
        {
            var serviceConcept = await _db.AdmConceptos.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CodConcepto == vm.ConceptCode);
            if (serviceConcept == null) return ServiceResult.FailureResult("Concepto inválido.");

            var branchName = await _db.AdmSucursales
                .Where(s => s.CodSucursal == vm.BranchCode)
                .Select(s => s.NombreSucursal)
                .FirstOrDefaultAsync();

            string cefEstado =
                (vm.ConceptCode == 1 || vm.ConceptCode == 4)
                ? nameof(CefTransactionStatusEnum.RegistroTesoreria)
                : (vm.ConceptCode == 2 || vm.ConceptCode == 3)
                ? nameof(CefTransactionStatusEnum.ProvisionEnProceso)
                : nameof(CefTransactionStatusEnum.RegistroTesoreria);

            // === Validaciones ===
            if (vm.ConceptCode == 5 && (vm.TransferType != "I" && vm.TransferType != "T"))
                return ServiceResult.FailureResult("Para TRASLADO, Tipo de Traslado debe ser 'Interno' o 'Transportadora'.");
            else if (vm.ConceptCode != 5)
                vm.TransferType = "N";

            if (vm.OriginIndicatorType == "P" && !await _db.AdmPuntos.AnyAsync(p => p.PointCode == vm.OriginPointCode))
                return ServiceResult.FailureResult($"Código de Punto de Origen '{vm.OriginPointCode}' inválido.");
            if (vm.OriginIndicatorType == "F" && !await _db.AdmFondos.AnyAsync(f => f.FundCode == vm.OriginPointCode))
                return ServiceResult.FailureResult($"Código de Fondo de Origen '{vm.OriginPointCode}' inválido.");
            if (vm.DestinationIndicatorType == "P" && !await _db.AdmPuntos.AnyAsync(p => p.PointCode == vm.DestinationPointCode))
                return ServiceResult.FailureResult($"Código de Punto de Destino '{vm.DestinationPointCode}' inválido.");
            if (vm.DestinationIndicatorType == "F" && !await _db.AdmFondos.AnyAsync(f => f.FundCode == vm.DestinationPointCode))
                return ServiceResult.FailureResult($"Código de Fondo de Destino '{vm.DestinationPointCode}' inválido.");

            var declaredBill = vm.BillValue ?? 0m;
            var declaredCoin = vm.CoinValue ?? 0m;
            var declaredDocs = 0m;
            var totalDeclared = vm.ServiceValue ?? 0m;
            var totalDeclaredLetters = AmountInWordsHelper.ToSpanishCurrency(totalDeclared, "COP");
            var totalCounted = 0;
            var totalCountedLetters = AmountInWordsHelper.ToSpanishCurrency(totalCounted, "COP");

            var declaredBags = vm.NumberOfCoinBags ?? 0;
            var declaredEnv = 0;
            var declaredChecks = 0;
            var declaredDocsCt = 0;
            int cefPlanilla = 0;
            var acceptanceDate = DateOnly.FromDateTime(DateTime.Now);
            var acceptanceTime = TimeOnly.FromDateTime(DateTime.Now);

            var p = new[]
            {
                // ===== CgsServicios =====
                new SqlParameter("@NumeroPedido",           (object?)vm.RequestNumber ?? DBNull.Value),
                new SqlParameter("@CodCliente",             vm.OriginClientCode),
                new SqlParameter("@CodOsCliente",           (object?)vm.ClientServiceOrderCode ?? DBNull.Value),
                new SqlParameter("@CodSucursal",            vm.BranchCode),
                new SqlParameter("@FechaSolicitud",         vm.RequestDate.ToDateTime(TimeOnly.MinValue)),
                new SqlParameter("@HoraSolicitud",          vm.RequestTime.ToTimeSpan()),
                new SqlParameter("@CodConcepto",            vm.ConceptCode),
                new SqlParameter("@TipoTraslado",           (object?)vm.TransferType ?? DBNull.Value),
                new SqlParameter("@CodEstado",              vm.StatusCode),
                new SqlParameter("@CodClienteOrigen",       (object?)vm.OriginClientCode ?? DBNull.Value),
                new SqlParameter("@CodPuntoOrigen",         vm.OriginPointCode),
                new SqlParameter("@IndicadorTipoOrigen",    vm.OriginIndicatorType),

                new SqlParameter("@CodClienteDestino",      (object?)vm.DestinationClientCode ?? DBNull.Value),
                new SqlParameter("@CodPuntoDestino",        vm.DestinationPointCode),
                new SqlParameter("@IndicadorTipoDestino",   vm.DestinationIndicatorType),

                new SqlParameter("@FechaAceptacion",        acceptanceDate),
                new SqlParameter("@HoraAceptacion",         acceptanceTime),
                new SqlParameter("@FechaProgramacion",      (object?)vm.ProgrammingDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraProgramacion",       (object?)vm.ProgrammingTime?.ToTimeSpan() ?? DBNull.Value),

                new SqlParameter("@FechaAtencionInicial",   (object?)vm.InitialAttentionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraAtencionInicial",    (object?)vm.InitialAttentionTime?.ToTimeSpan() ?? DBNull.Value),
                new SqlParameter("@FechaAtencionFinal",     (object?)vm.FinalAttentionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraAtencionFinal",      (object?)vm.FinalAttentionTime?.ToTimeSpan() ?? DBNull.Value),

                new SqlParameter("@FechaCancelacion",       (object?)vm.CancellationDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraCancelacion",        (object?)vm.CancellationTime?.ToTimeSpan() ?? DBNull.Value),
                new SqlParameter("@FechaRechazo",           (object?)vm.RejectionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraRechazo",            (object?)vm.RejectionTime?.ToTimeSpan() ?? DBNull.Value),

                new SqlParameter("@Fallido",                vm.IsFailed),
                new SqlParameter("@ResponsableFallido",     (object?)vm.FailedResponsible ?? DBNull.Value),
                new SqlParameter("@RazonFallido",           (object?)vm.FailedReason ?? DBNull.Value),

                new SqlParameter("@PersonaCancelacion",     (object?)vm.CancellationPerson ?? DBNull.Value),
                new SqlParameter("@OperadorCancelacion",    (object?)vm.CancellationOperator ?? DBNull.Value),
                new SqlParameter("@MotivoCancelacion",      (object?)vm.CancellationReason ?? DBNull.Value),

                new SqlParameter("@ModalidadServicio",      (object?)vm.ServiceModality ?? DBNull.Value),
                new SqlParameter("@Observaciones",          (object?)vm.Observations ?? DBNull.Value),
                new SqlParameter("@Clave",                  (object?)vm.KeyValue ?? DBNull.Value),
                new SqlParameter("@OperadorCgsId",          userId),
                new SqlParameter("@SucursalCgs",            branchName),
                new SqlParameter("@IpOperador",             (object?)ip ?? DBNull.Value),

                new SqlParameter("@ValorBillete",           (object?)vm.BillValue ?? 0m),
                new SqlParameter("@ValorMoneda",            (object?)vm.CoinValue ?? 0m),
                new SqlParameter("@ValorServicio",          totalDeclared),
                new SqlParameter("@NumeroKitsCambio",       (object?)vm.NumberOfChangeKits ?? 0),
                new SqlParameter("@NumeroBolsasMoneda",     (object?)vm.NumberOfCoinBags ?? 0),
                new SqlParameter("@ArchivoDetalle",         (object?)vm.DetailFile ?? DBNull.Value),

                // ===== CefTransacciones =====
                new SqlParameter("@CefCodRuta",                    (object?)DBNull.Value),
                new SqlParameter("@CefNumeroPlanilla",             cefPlanilla),
                new SqlParameter("@CefDivisa",                     (object?)vm.Currency ?? DBNull.Value),
                new SqlParameter("@CefTipoTransaccion",            serviceConcept.TipoConcepto),
                new SqlParameter("@CefNumeroMesaConteo",           (object?)DBNull.Value),
                new SqlParameter("@CefCantidadBolsasDeclaradas",   declaredBags),
                new SqlParameter("@CefCantidadSobresDeclarados",   declaredEnv),
                new SqlParameter("@CefCantidadChequesDeclarados",  declaredChecks),
                new SqlParameter("@CefCantidadDocumentosDeclarados", declaredDocsCt),
                new SqlParameter("@CefValorBilletesDeclarado",     declaredBill),
                new SqlParameter("@CefValorMonedasDeclarado",      declaredCoin),
                new SqlParameter("@CefValorDocumentosDeclarado",   declaredDocs),
                new SqlParameter("@CefValorTotalDeclarado",        totalDeclared),
                new SqlParameter("@CefValorTotalDeclaradoLetras",  (object?)totalDeclaredLetters ?? DBNull.Value),
                new SqlParameter("@CefValorTotalContadoLetras",   (object?)totalCountedLetters ?? DBNull.Value),
                new SqlParameter("@CefNovedadInformativa",         (object?)DBNull.Value),
                new SqlParameter("@CefEsCustodia",                 false),
                new SqlParameter("@CefEsPuntoAPunto",              false),
                new SqlParameter("@CefEstadoTransaccion",          cefEstado),
                new SqlParameter("@CefFechaRegistro",              DateTime.Now),
                new SqlParameter("@CefUsuarioRegistroId",          userId),
                new SqlParameter("@CefIPRegistro",                 (object?)ip ?? DBNull.Value),
                new SqlParameter("@CefReponsableEntregaId",        (object?)DBNull.Value),
                new SqlParameter("@CefResponsableRecibeId",        (object?)DBNull.Value),
            };

            var sql = "EXEC dbo.AddServiceTransaction " + string.Join(", ", p.Select(x => x.ParameterName));
            var list = await _db.Database.SqlQueryRaw<string>(sql, p).ToListAsync();
            var order = list.FirstOrDefault();

            return !string.IsNullOrWhiteSpace(order)
                ? ServiceResult.SuccessResult($"Servicio creado: {order}", order)
                : ServiceResult.FailureResult("No se pudo generar la Orden de Servicio.");
        }

        private static int MapLocationType(string? t)
        {
            return t switch
            {
                "P" => 0, // Punto/Oficina
                "A" => 1, // ATM
                "F" => 2, // Fondo
                _ => 0
            };
        }
    }
}
