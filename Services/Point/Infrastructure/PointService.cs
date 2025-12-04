using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VCashApp.Data;
using VCashApp.Models.Dtos.Point;
using VCashApp.Models.Entities;
using VCashApp.Extensions;
using VCashApp.Services.DTOs;
using VCashApp.Services.Point.Application;

namespace VCashApp.Services.Point.Infrastructure
{
    public sealed class PointService : IPointService
    {
        private readonly AppDbContext _db;
        private readonly PointFileManager _fileMgr;

        public PointService(AppDbContext db, IOptions<RepositorioOptions> repoOpts)
        {
            _db = db;
            _fileMgr = new PointFileManager(repoOpts);
        }

        // ============================================================
        // CREATE
        // ============================================================
        public async Task<ServiceResult> CreateAsync(PointUpsertDto dto, IFormFile? cartaFile)
        {
            var valid = await ValidateAsync(dto, isEdit: false);
            if (!valid.Success)
                return valid;

            var point = new AdmPunto
            {
                PointCode = dto.CodPunto.Trim(),
                ClientPointCode = dto.CodPCliente.Trim(),
                ClientCode = dto.CodCliente,
                MainClientCode = dto.CodClientePpal,

                PointName = dto.NombrePunto,
                ShortName = dto.NombreCorto,
                BillingPoint = dto.NombrePuntoFact,
                Address = dto.DirPunto,
                PhoneNumber = dto.TelPunto,
                Responsible = dto.RespPunto,
                ResponsiblePosition = dto.CargoRespPunto,
                ResponsibleEmail = dto.CorreoRespPunto,

                BranchCode = dto.CodSuc,
                CityCode = dto.CodCiudad,

                Latitude = dto.LatPunto,
                Longitude = dto.LngPunto,
                PointRadius = dto.RadioPunto?.ToString(),

                ChangeBase = dto.BaseCambio == 1,
                PointKeys = dto.LlavesPunto == 1,
                PointEnvelopes = dto.SobresPunto == 1,
                PointChecks = dto.ChequesPunto == 1,
                PointDocuments = dto.DocumentosPunto == 1,
                PointStock = dto.ExistenciasPunto == 1,
                PointPrediction = dto.PrediccionPunto == 1,
                PointCustody = dto.CustodiaPunto == 1,
                PointOtherValues = dto.OtrosValoresPunto == 1,

                Others = dto.Otros,
                CashReleasePoint = dto.LiberacionEfectivoPunto == 1,

                PointFund = dto.FondoPunto,
                FundCode = dto.CodFondo,
                RouteBranchCode = dto.CodRutaSuc,
                RangeCode = dto.CodRango,

                EntryDate = dto.FecIngreso,
                WithdrawalDate = dto.FecRetiro,

                Cas4uCode = $"{dto.CodCliente}|{dto.CodPCliente}",
                RiskLevel = dto.NivelRiesgo,
                PointCoverage = dto.CoberturaPunto,
                InterurbanScale = dto.EscalaInterurbanos,

                Consignment = dto.Consignacion,
                Status = dto.EstadoPunto
            };

            // Guardar archivo si viene adjunto
            if (cartaFile != null)
            {
                string savedName = await _fileMgr.SaveCartaAsync(dto.CodCliente, dto.CodPCliente, cartaFile);
                point.InclusionLetter = savedName;
            }

            _db.AdmPuntos.Add(point);
            await _db.SaveChangesAsync();

            return ServiceResult.SuccessResult("Punto creado correctamente.");
        }

        // ============================================================
        // UPDATE
        // ============================================================
        public async Task<ServiceResult> UpdateAsync(PointUpsertDto dto, IFormFile? cartaFile, bool removeCartaActual)
        {
            var valid = await ValidateAsync(dto, isEdit: true);
            if (!valid.Success)
                return valid;

            var point = await _db.AdmPuntos.FirstOrDefaultAsync(p => p.PointCode == dto.CodPunto);
            if (point == null)
                return ServiceResult.FailureResult("Punto no encontrado.");

            // Actualizar info
            point.ClientPointCode = dto.CodPCliente.Trim();
            point.ClientCode = dto.CodCliente;
            point.MainClientCode = dto.CodClientePpal;

            point.PointName = dto.NombrePunto;
            point.ShortName = dto.NombreCorto;
            point.BillingPoint = dto.NombrePuntoFact;
            point.Address = dto.DirPunto;
            point.PhoneNumber = dto.TelPunto;

            point.Responsible = dto.RespPunto;
            point.ResponsiblePosition = dto.CargoRespPunto;
            point.ResponsibleEmail = dto.CorreoRespPunto;

            point.BranchCode = dto.CodSuc;
            point.CityCode = dto.CodCiudad;

            point.Latitude = dto.LatPunto;
            point.Longitude = dto.LngPunto;
            point.PointRadius = dto.RadioPunto?.ToString();

            point.ChangeBase = dto.BaseCambio == 1;
            point.PointKeys = dto.LlavesPunto == 1;
            point.PointEnvelopes = dto.SobresPunto == 1;
            point.PointChecks = dto.ChequesPunto == 1;

            point.PointDocuments = dto.DocumentosPunto == 1;
            point.PointStock = dto.ExistenciasPunto == 1;
            point.PointPrediction = dto.PrediccionPunto == 1;
            point.PointCustody = dto.CustodiaPunto == 1;
            point.PointOtherValues = dto.OtrosValoresPunto == 1;

            point.Others = dto.Otros;
            point.CashReleasePoint = dto.LiberacionEfectivoPunto == 1;

            point.PointFund = dto.FondoPunto;
            point.FundCode = dto.CodFondo;
            point.RouteBranchCode = dto.CodRutaSuc;
            point.RangeCode = dto.CodRango;

            point.EntryDate = dto.FecIngreso;
            point.WithdrawalDate = dto.FecRetiro;

            point.Cas4uCode = $"{dto.CodCliente}|{dto.CodPCliente}";
            point.RiskLevel = dto.NivelRiesgo;
            point.PointCoverage = dto.CoberturaPunto;
            point.InterurbanScale = dto.EscalaInterurbanos;

            point.Consignment = dto.Consignacion;
            point.Status = dto.EstadoPunto;

            // Eliminación manual
            if (removeCartaActual)
            {
                _fileMgr.DeleteCarta(dto.CodCliente, point.InclusionLetter);
                point.InclusionLetter = null;
            }

            // Reemplazo de archivo
            if (cartaFile != null)
            {
                _fileMgr.DeleteCarta(dto.CodCliente, point.InclusionLetter);
                string savedName = await _fileMgr.SaveCartaAsync(dto.CodCliente, dto.CodPCliente, cartaFile);
                point.InclusionLetter = savedName;
            }

            await _db.SaveChangesAsync();

            return ServiceResult.SuccessResult("Punto actualizado correctamente.");
        }

        // ============================================================
        // TOGGLE STATUS
        // ============================================================
        public async Task<ServiceResult> ToggleStatusAsync(string codPunto)
        {
            var p = await _db.AdmPuntos.FirstOrDefaultAsync(p => p.PointCode == codPunto);
            if (p == null)
                return ServiceResult.FailureResult("Punto no encontrado.");

            p.Status = !p.Status;
            await _db.SaveChangesAsync();

            return ServiceResult.SuccessResult("Estado actualizado.");
        }

        // ============================================================
        // GENERACIÓN DE CÓDIGO
        // ============================================================
        public async Task<string> GenerateCodPuntoAsync(int codCliente)
        {
            var last = await _db.AdmPuntos
                .Where(p => p.ClientCode == codCliente)
                .OrderByDescending(p => p.PointCode)
                .Select(p => p.PointCode)
                .FirstOrDefaultAsync();

            if (last == null)
                return $"{codCliente}-001";

            var parts = last.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int num))
                return $"{codCliente}-001";

            int next = num + 1;
            return $"{codCliente}-{next:D3}";
        }

        // ============================================================
        // VALIDACIÓN GENERAL
        // ============================================================
        public async Task<ServiceResult> ValidateAsync(PointUpsertDto dto, bool isEdit)
        {
            if (string.IsNullOrWhiteSpace(dto.CodPunto))
                return ServiceResult.FailureResult("CodPunto vacío.");

            if (string.IsNullOrWhiteSpace(dto.CodPCliente))
                return ServiceResult.FailureResult("CodPCliente vacío.");

            string codPunto = dto.CodPunto.Trim();

            if (!isEdit)
            {
                bool exists = await _db.AdmPuntos.AnyAsync(p => p.PointCode == codPunto);
                if (exists)
                    return ServiceResult.FailureResult("Ya existe un punto con este código.");
            }

            return ServiceResult.SuccessResult("OK.");
        }
    }
}