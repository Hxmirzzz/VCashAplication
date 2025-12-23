using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using VCashApp.Data;
using VCashApp.Extensions;
using VCashApp.Models.Dtos.Point;
using VCashApp.Models.Entities;
using VCashApp.Services.DTOs;
using VCashApp.Services.Point.Application;

namespace VCashApp.Services.Point.Infrastructure
{
    /// <summary>Manejo de puntos.</summary>
    public sealed class PointService : IPointService
    {
        private readonly AppDbContext _db;
        private readonly PointFileManager _fileMgr;
        /// <summary>Constructor.</summary>
        public PointService(AppDbContext db, IOptions<RepositorioOptions> repoOpts)
        {
            _db = db;
            _fileMgr = new PointFileManager(repoOpts);
        }

        /// <summary>
        /// Método asincrono que crea un nuevo punto.
        /// </summary>
        /// <param name="dto">Datos del punto.</param>
        /// <param name="cartaFile">Archivo.</param>
        /// <returns></returns>
        public async Task<ServiceResult> CreateAsync(PointUpsertDto dto, IFormFile? cartaFile)
        {
            var valid = await ValidateAsync(dto, isEdit: false);
            if (!valid.Success)
                return valid;

            var point = new AdmPunto
            {
                PointCode = dto.CodPunto.Trim(),
                VatcoPointCode = dto.VatcoPointCode?.Trim(),
                ClientPointCode = dto.CodPCliente.Trim(),
                ClientCode = dto.CodCliente,
                MainClientCode = dto.CodClientePpal ?? 0,

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
                PointType = dto.TipoPunto,
                BusinessType = dto.BusinessType,

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

        /// <summary>
        /// Método asincrono que actualiza un punto existente.
        /// </summary>
        /// <param name="dto">Datos del punto.</param>
        /// <param name="cartaFile">Archivo.</param>
        /// <param name="removeCartaActual">Remover archivo actual.</param>
        /// <returns>Devuelve resultado del servicio.</returns>
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

        /// <summary>
        /// Método asincrono que activa o desactiva un punto.
        /// </summary>
        /// <param name="codPunto">Codigo del punto.</param>
        /// <returns>Devuelve resultado del servicio.</returns>
        public async Task<ServiceResult> ToggleStatusAsync(string codPunto)
        {
            var p = await _db.AdmPuntos.FirstOrDefaultAsync(p => p.PointCode == codPunto);
            if (p == null)
                return ServiceResult.FailureResult("Punto no encontrado.");

            p.Status = !p.Status;
            await _db.SaveChangesAsync();

            return ServiceResult.SuccessResult("Estado actualizado.");
        }

        /// <summary>
        /// Método asincrono que valida los datos antes de crear o actualizar un punto.
        /// </summary>
        /// <param name="dto">Datos del punto.</param>
        /// <param name="isEdit">Verdadero si es edición, falso si es creación.</param>
        /// <returns>Devuelve resultado de la validación.</returns>
        public async Task<ServiceResult> ValidateAsync(PointUpsertDto dto, bool isEdit)
        {
            if (string.IsNullOrWhiteSpace(dto.CodPunto))
                return ServiceResult.FailureResult("CodPunto vacío.");

            if (string.IsNullOrWhiteSpace(dto.CodPCliente))
                return ServiceResult.FailureResult("CodPCliente vacío.");

            if (string.IsNullOrWhiteSpace(dto.NombrePunto))
                return ServiceResult.FailureResult("NombrePunto vacío.");

            string codPunto = dto.CodPunto.Trim();

            if (dto.FondoPunto == 1)
            {
                if (string.IsNullOrWhiteSpace(dto.CodFondo))
                    return ServiceResult.FailureResult("Debe seleccionar un fondo si 'FondoPunto' está activado.");
            }
            else
            {
                dto.CodFondo = null;
            }

            if (dto.CodSuc <= 0)
                return ServiceResult.FailureResult("Debe seleccionar una 'Sucursal' válida.");

            if (dto.CodCiudad <= 0)
                return ServiceResult.FailureResult("Debe seleccionar una 'Ciudad' válida.");

            if (string.IsNullOrWhiteSpace(dto.CodRutaSuc))
                return ServiceResult.FailureResult("Debe seleccionar una 'Ruta' válida.");

            if (dto.CodRango <= 0)
                return ServiceResult.FailureResult("Debe seleccionar un 'Rango' válido.");

            if (dto.BusinessType == null || dto.BusinessType <= 0)
                return ServiceResult.FailureResult("Debe seleccionar un 'Tipo de negocio' válido.");

            if (!isEdit)
            {
                bool exists = await _db.AdmPuntos.AnyAsync(p => p.PointCode == codPunto);
                if (exists)
                    return ServiceResult.FailureResult("Ya existe un punto con este código.");
            }

            return ServiceResult.SuccessResult("OK.");
        }

        /// <summary>
        /// Método asincrono que genera el código interno CodPunto según reglas (incremental o lógica propia).
        /// </summary>
        /// <param name="codCliente">Cliente.</param>
        /// <param name="tipoPunto">Tipo de punto</param>
        /// <returns>Devuelve el código generado.</returns>
        public async Task<string> GenerateVatcoCodeAsync(int codCliente, int tipoPunto)
        {
            // prefijo inicial según tipo
            int prefijo = tipoPunto == 0 ? 0 : 6;
            int maxNumero = tipoPunto == 0 ? 999 : 1999;

            // Traer último código VATCO por cliente + tipo
            var lastCode = await _db.AdmPuntos
                .Where(p =>
                    p.ClientCode == codCliente &&
                    p.PointType == tipoPunto &&
                    p.VatcoPointCode != null)
                .OrderByDescending(p => p.VatcoPointCode)
                .Select(p => p.VatcoPointCode!)
                .FirstOrDefaultAsync();

            int nuevoNumero = 1;

            if (!string.IsNullOrEmpty(lastCode) &&
                lastCode.StartsWith(codCliente.ToString()))
            {
                // Estructura: {codCliente}{prefijo}{NNN}
                var clienteLen = codCliente.ToString().Length;

                int ultimoPrefijo = int.Parse(lastCode.Substring(clienteLen, 1));
                int ultimoNumero = int.Parse(lastCode.Substring(clienteLen + 1));

                if (ultimoNumero >= maxNumero)
                {
                    prefijo = ultimoPrefijo + 1;
                    nuevoNumero = 0;
                }
                else
                {
                    prefijo = ultimoPrefijo;
                    nuevoNumero = ultimoNumero + 1;
                }
            }

            return $"{codCliente}{prefijo}{nuevoNumero:D3}";
        }

        /// <summary>
        /// Método asincrono que obtiene las opciones de clientes principales para un cliente dado.
        /// </summary>
        /// <param name="codCliente">Cliente.</param>
        /// <returns>Devuelve lista de opciones.</returns>
        public async Task<IReadOnlyList<MainClientOptionDto>> GetMainClientOptionsAsync(int codCliente)
        {
            var cliente = await _db.AdmClientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientCode == codCliente);

            if (cliente == null)
            {
                return new[]
                {
                    new MainClientOptionDto
                    {
                        Value = 0,
                        Text = "NINGUNO",
                        Selected = true,
                        LockSelect = true
                    }
                };
            }

            // DIRECTO
            if (cliente.ClientType == 1 || cliente.MainClient == 0)
            {
                return new[]
                {
                    new MainClientOptionDto
                    {
                        Value = 0,
                        Text = "NINGUNO",
                        Selected = true,
                        LockSelect = true
                    }
                };
            }

            var principal = await _db.AdmClientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientCode == cliente.MainClient);

            if (principal == null)
            {
                return new[]
                {
                    new MainClientOptionDto
                    {
                        Value = 0,
                        Text = "NINGUNO",
                        Selected = true,
                        LockSelect = true
                    }
                };
            }

            // AMPARADO
            if (cliente.ClientType == 2)
            {
                return new[]
                {
                    new MainClientOptionDto
                    {
                        Value = principal.ClientCode,
                        Text = $"PPAL: {principal.ClientCode} — {principal.ClientName}",
                        Selected = true,
                        LockSelect = true
                    }
                };
            }

            // MIXTO
            return new[]
            {
                new MainClientOptionDto
                {
                    Value = 0,
                    Text = "NINGUNO",
                    Selected = true,
                    LockSelect = false
                },
                new MainClientOptionDto
                {
                    Value = principal.ClientCode,
                    Text = $"PPAL: {principal.ClientCode} — {principal.ClientName}",
                    Selected = false,
                    LockSelect = false
                }
            };
        }

        /// <summary>
        /// Obtiene el HTML con las opciones de fondos asignados a un punto.
        /// </summary>
        /// <param name="branchId">Sucursal.</param>
        /// <param name="clientId">Cliente.</param>
        /// <param name="mainClientId">Cliente Principal.</param>
        /// <returns>Lista HTML de fondos.</returns>
        public async Task<string> GetFundsOptionsHtmlAsync(int branchId, int clientId, int mainClientId)
        {
            if (branchId <= 0)
                return "<option value=''>-- Seleccione fondo --</option>";

            var query = _db.AdmFondos
                .AsNoTracking()
                .Where(f =>
                    f.BranchCode == branchId &&
                    f.FundStatus &&
                    (
                        f.ClientCode == clientId ||
                        (mainClientId > 0 && f.ClientCode == mainClientId)
                    )
                )
                .OrderBy(f => f.FundName)
                .Select(f => new
                {
                    f.FundCode,
                    f.FundName
                });

            var fondos = await query.ToListAsync();

            var sb = new StringBuilder();
            sb.Append("<option value=''>-- Seleccione fondo --</option>");

            foreach (var f in fondos)
            {
                sb.AppendFormat(
                    "<option value=\"{0}\">{1}</option>",
                    f.FundCode,
                    System.Net.WebUtility.HtmlEncode(f.FundName)
                );
            }

            return sb.ToString();
        }

        /// <summary>
        /// Metodo asincrono que obtiene el html de las rutas de un punto.
        /// </summary>
        /// <param name="branchId">Sucursal.</param>
        /// <returns>Lista HTML de rutas.</returns>
        public async Task<string> GetRoutesOptionsHtmlAsync(int branchId)
        {
            if (branchId <= 0)
                return "<option value=''>-- Seleccione ruta --</option>";
            var query = _db.AdmRutas
                .AsNoTracking()
                .Where(r =>
                    r.BranchId == branchId &&
                    r.Status)
                .OrderBy(r => r.RouteName)
                .Select(r => new
                {
                    r.BranchRouteCode,
                    r.RouteName
                });
            var rutas = await query.ToListAsync();
            var sb = new StringBuilder();
            sb.Append("<option value=''>-- Seleccione ruta --</option>");
            foreach (var r in rutas)
            {
                sb.AppendFormat(
                    "<option value=\"{0}\">{1}</option>",
                    r.BranchRouteCode,
                    System.Net.WebUtility.HtmlEncode(r.RouteName)
                );
            }
            return sb.ToString();
        }

        /// <summary>
        /// Obtiene el HTML con las opciones de rangos asignados a un punto.
        /// </summary>
        /// <param name="clientId">Cliente.</param>
        /// <returns>Devuelve lista HTML de rangos.</returns>
        public async Task<string> GetRangeOptionsHtmlAsync(int clientId)
        {
            if (clientId <= 0)
                return "<option value=''>-- Seleccione rango --</option>";
            var query = _db.AdmRangos
                .AsNoTracking()
                .Where(r =>
                    r.ClientId == clientId &&
                    r.RangeStatus)
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.CodRange
                });
            var rangos = await query.ToListAsync();
            var sb = new StringBuilder();
            sb.Append("<option value=''>-- Seleccione rango --</option>");
            foreach (var r in rangos)
            {
                sb.AppendFormat(
                    "<option value=\"{0}\">{1}</option>",
                    r.Id,
                    System.Net.WebUtility.HtmlEncode(r.CodRange)
                );
            }
            return sb.ToString();
        }

        /// <summary>
        /// Obtiene el HTML con la información de un rango.
        /// </summary>
        /// <param name="rangeId">Rango.</param>
        /// <returns>Lista HTML de información del rango.</returns>
        public async Task<string> GetRangeInfoHtmlAsync(int rangeId)
        {
            var rango = await _db.AdmRangos
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == rangeId && r.RangeStatus);

            if (rango == null)
                return "<div class='text-muted'>No hay información disponible</div>";

            var rows = new List<RangeDayRow>
            {
                Build("Lunes", rango.Lr1Hi, rango.Lr1Hf, rango.Lr2Hi, rango.Lr2Hf, rango.Lr3Hi, rango.Lr3Hf),
                Build("Martes", rango.Mr1Hi, rango.Mr1Hf, rango.Mr2Hi, rango.Mr2Hf, rango.Mr3Hi, rango.Mr3Hf),
                Build("Miércoles", rango.Wr1Hi, rango.Wr1Hf, rango.Wr2Hi, rango.Wr2Hf, rango.Wr3Hi, rango.Wr3Hf),
                Build("Jueves", rango.Jr1Hi, rango.Jr1Hf, rango.Jr2Hi, rango.Jr2Hf, rango.Jr3Hi, rango.Jr3Hf),
                Build("Viernes", rango.Vr1Hi, rango.Vr1Hf, rango.Vr2Hi, rango.Vr2Hf, rango.Vr3Hi, rango.Vr3Hf),
                Build("Sábado", rango.Sr1Hi, rango.Sr1Hf, rango.Sr2Hi, rango.Sr2Hf, rango.Sr3Hi, rango.Sr3Hf),
                Build("Domingo", rango.Dr1Hi, rango.Dr1Hf, rango.Dr2Hi, rango.Dr2Hf, rango.Dr3Hi, rango.Dr3Hf),
                Build("Festivo", rango.Fr1Hi, rango.Fr1Hf, rango.Fr2Hi, rango.Fr2Hf, rango.Fr3Hi, rango.Fr3Hf),
            };

            return BuildHtml(rows);
        }

        private static RangeDayRow Build(
            string day,
            TimeSpan? h1i, TimeSpan? h1f,
            TimeSpan? h2i, TimeSpan? h2f,
            TimeSpan? h3i, TimeSpan? h3f)
        {
            return new RangeDayRow
            {
                DayName = day,
                RangeOne = Format(h1i, h1f),
                RangeTwo = Format(h2i, h2f),
                RangeThree = Format(h3i, h3f)
            };
        }

        private static string? Format(TimeSpan? hi, TimeSpan? hf)
        {
            if (hi == null || hf == null)
                return null;
            return $"{hi:hh\\:mm} - {hf:hh\\:mm}";
        }

        private static string BuildHtml(List<RangeDayRow> rows)
        {
            var sb = new StringBuilder();

            sb.Append("""
            <table class="table table-sm table-bordered text-center align-middle">
                <thead class="table-light">
                    <tr>
                        <th>Día</th>
                        <th>Rango 1</th>
                        <th>Rango 2</th>
                        <th>Rango 3</th>
                    </tr>
                </thead>
                <tbody>
            """);

            foreach (var r in rows)
            {
                sb.Append($"""
                <tr>
                    <td><strong>{r.DayName}</strong></td>
                    <td>{r.RangeOne ?? "-"}</td>
                    <td>{r.RangeTwo ?? "-"}</td>
                    <td>{r.RangeThree ?? "-"}</td>
                </tr>
                """);
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }
    }
}