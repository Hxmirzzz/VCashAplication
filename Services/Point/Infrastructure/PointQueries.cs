using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models.Dtos.Point;
using VCashApp.Models.Entities;
using VCashApp.Services.Point.Application;

namespace VCashApp.Services.Point.Infrastructure
{
    /// <summary>
    /// Métodos de consulta para puntos.
    /// </summary>
    public sealed class PointQueries : IPointQueries
    {
        private readonly AppDbContext _db;
        private readonly IBranchContext _branchCtx;

        /// <summary>
        /// Metódos de consulta para puntos.
        /// </summary>
        /// <param name="db">Contexto de base de datos.</param>
        /// <param name="branchCtx">Servicio de contexto de sucursal.</param>
        public PointQueries(AppDbContext db, IBranchContext branchCtx)
        {
            _db = db;
            _branchCtx = branchCtx;
        }

        // --------------------------------------------------------------------
        // 1) LISTADO PAGINADO CON FILTROS
        // --------------------------------------------------------------------
        /// <summary>
        /// Obtiene un listado paginado de puntos según los filtros indicados.
        /// </summary>
        /// <param name="filter">DTO</param>
        /// <returns>DTO con items y total</returns>
        public async Task<(IEnumerable<PointListDto> Items, int TotalCount)> GetPagedAsync(PointFilterDto filter)
        {
            IQueryable<AdmPunto> q = _db.AdmPuntos.AsNoTracking();

            int? effectiveBranch = filter.BranchCode ?? _branchCtx.CurrentBranchId;

            if (_branchCtx.AllBranches)
            {
                if (_branchCtx.PermittedBranchIds?.Any() == true)
                    q = q.Where(f => f.BranchCode.HasValue && _branchCtx.PermittedBranchIds.Contains(f.BranchCode.Value));

                if (effectiveBranch.HasValue)
                    q = q.Where(f => f.BranchCode == effectiveBranch.Value);
            }
            else
            {
                if (_branchCtx.CurrentBranchId.HasValue)
                    q = q.Where(f => f.BranchCode == _branchCtx.CurrentBranchId.Value);
            }

            q = q.Where(p => p.PointType == 0);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string s = filter.Search.Trim().ToLower();
                q = q.Where(p =>
                    p.PointCode!.Contains(s) ||
                    p.ClientPointCode!.Contains(s) ||
                    p.PointName!.Contains(s) ||
                    p.ShortName!.Contains(s) ||
                    p.Client!.ClientName.Contains(s));
            }

            if (filter.ClientCode.HasValue)
                q = q.Where(p => p.ClientCode == filter.ClientCode);

            if (filter.MainClientCode.HasValue)
                q = q.Where(p => p.MainClientCode == filter.MainClientCode);

            if (filter.CityCode.HasValue)
                q = q.Where(p => p.CityCode == filter.CityCode);

            if (filter.FundCode is not null)
                q = q.Where(p => p.FundCode == filter.FundCode);

            if (filter.RouteCode is not null)
                q = q.Where(p => p.RouteBranchCode == filter.RouteCode);

            if (filter.RangeCode is not null)
                q = q.Where(p => p.RangeCode == filter.RangeCode);

            if (filter.IsActive.HasValue)
                q = q.Where(p => p.Status == filter.IsActive.Value);

            int total = await q.CountAsync();

            q = q.Include(p => p.Client)
                 .Include(p => p.Branch)
                 .Include(p => p.City)
                 .Include(p => p.Fund)
                 .Include(p => p.Route)
                 .Include(p => p.Range);

            q = q.OrderBy(p => p.Client!.ClientName)
                 .ThenBy(p => p.PointName)
                 .ThenBy(p => p.City!.NombreCiudad)
                 .ThenBy(p => p.Branch!.NombreSucursal)
                 .Skip((filter.Page - 1) * filter.PageSize)
                 .Take(filter.PageSize);

            var items = await q.Select(p => new PointListDto
            {
                CodPunto = p.PointCode!,
                CodPCliente = p.ClientPointCode ?? "",
                CodCliente = p.ClientCode ?? 0,
                ClienteNombre = p.Client!.ClientName,
                CodClientePpal = p.MainClientCode ?? 0,
                ClientePpalNombre = null,
                NombrePunto = p.PointName,
                NombreCorto = p.ShortName,
                CodSuc = p.BranchCode ?? 0,
                NombreSucursal = p.Branch!.NombreSucursal,
                CodCiudad = p.CityCode ?? 0,
                NombreCiudad = p.City!.NombreCiudad,
                FundName = p.Fund!.FundName,
                RouteName = p.Route!.RouteName,
                RangeName = p.Range!.RangeInformation,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                PointRadius = p.PointRadius,
                EstadoPunto = p.Status
            }).ToListAsync();

            return (items, total);
        }

        /// <summary>
        /// Método para exportar los puntos según los filtros indicados.
        /// </summary>
        /// <param name="filter">DTO</param>
        /// <returns>DTO con items</returns>
        public async Task<IEnumerable<PointListDto>> ExportAsync(PointFilterDto filter)
        {
            var (items, _) = await GetPagedAsync(new PointFilterDto
            {
                Search = filter.Search,
                BranchCode = filter.BranchCode,
                CityCode = filter.CityCode,
                ClientCode = filter.ClientCode,
                MainClientCode = filter.MainClientCode,
                FundCode = filter.FundCode,
                RouteCode = filter.RouteCode,
                RangeCode = filter.RangeCode,
                IsActive = filter.IsActive,
                Page = 1,
                PageSize = int.MaxValue
            });

            return items;
        }

        /// <summary>
        /// Obtiene los datos para los select de búsqueda y edición de puntos.
        /// </summary>
        /// <returns>DTO con los datos de los select.</returns>
        public async Task<PointLookupDto> GetLookupsAsync()
        {
            // --- Clientes ---
            var clientes = await _db.AdmClientes
                .AsNoTracking()
                .OrderBy(c => c.ClientName)
                .Where(c => c.Status)
                .Select(c => new SelectListItem
                {
                    Value = c.ClientCode.ToString(),
                    Text = c.ClientName
                })
                .ToListAsync();

            // --- Sucursales ---
            var sucursalesQ = _db.AdmSucursales.AsNoTracking();

            if (!_branchCtx.AllBranches)
                sucursalesQ = sucursalesQ
                    .Where(s => _branchCtx.PermittedBranchIds.Contains(s.CodSucursal));

            var sucursales = await sucursalesQ
                .OrderBy(s => s.NombreSucursal)
                .Where(s => s.Estado && s.CodSucursal != 32)
                .Select(s => new SelectListItem
                {
                    Value = s.CodSucursal.ToString(),
                    Text = s.NombreSucursal
                }).ToListAsync();

            // --- Ciudades ---
            var ciudades = await _db.AdmCiudades
                .AsNoTracking()
                .OrderBy(c => c.NombreCiudad)
                .Select(c => new SelectListItem
                {
                    Value = c.CodCiudad.ToString(),
                    Text =  c.NombreCiudad + " (" + c.Departamento!.NombreDepartamento + ")"
                }).ToListAsync();

            // --- Fondos filtrados por regla VCash2 ---
            var fondos = await _db.AdmFondos
                .AsNoTracking()
                .OrderBy(f => f.FundName)
                .Select(f => new SelectListItem
                {
                    Value = f.FundCode!,
                    Text = f.FundName!
                }).ToListAsync();

            // --- Rutas ---
            var rutas = await _db.AdmRutas
                .AsNoTracking()
                .OrderBy(r => r.RouteName)
                .Select(r => new SelectListItem
                {
                    Value = r.BranchRouteCode!,
                    Text = r.RouteName!
                }).ToListAsync();

            // --- Rangos ---
            var rangos = await _db.AdmRangos
                .AsNoTracking()
                .OrderBy(r => r.RangeInformation)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.CodRange!
                }).ToListAsync();

            var tiposNegocio = await _db.AdmTypeBusinesses
                .AsNoTracking()
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Description!
                }).ToListAsync();

            return new PointLookupDto
            {
                Clientes = clientes,
                Sucursales = sucursales,
                Ciudades = ciudades,
                Fondos = fondos,
                Rutas = rutas,
                Rangos = rangos,
                TiposNegocio = tiposNegocio
            };
        }

        /// <summary>
        /// Obtiene los datos de un punto para edición.
        /// </summary>
        /// <param name="pointCode">Punto</param>
        /// <returns>Datos del punto para edición.</returns>
        public async Task<PointUpsertDto?> GetForEditAsync(string pointCode)
        {
            return await _db.AdmPuntos
                .AsNoTracking()
                .Where(p => p.PointCode == pointCode)
                .Select(p => new PointUpsertDto
                {
                    CodPunto = p.PointCode,
                    CodPCliente = p.ClientPointCode!,
                    CodCliente = p.ClientCode ?? 0,
                    VatcoPointCode = p.VatcoPointCode,
                    CodClientePpal = p.MainClientCode,

                    NombrePunto = p.PointName,
                    NombreCorto = p.ShortName,
                    NombrePuntoFact = p.BillingPoint,
                    DirPunto = p.Address,
                    TelPunto = p.PhoneNumber,

                    RespPunto = p.Responsible,
                    CargoRespPunto = p.ResponsiblePosition,
                    CorreoRespPunto = p.ResponsibleEmail,

                    CodSuc = p.BranchCode ?? 0,
                    CodCiudad = p.CityCode ?? 0,

                    LatPunto = p.Latitude,
                    LngPunto = p.Longitude,
                    RadioPunto = p.PointRadius != null ? int.Parse(p.PointRadius) : null,

                    BaseCambio = p.ChangeBase ? 1 : 0,
                    LlavesPunto = p.PointKeys ? 1 : 0,
                    SobresPunto = p.PointEnvelopes ? 1 : 0,
                    ChequesPunto = p.PointChecks ? 1 : 0,

                    DocumentosPunto = p.PointDocuments ? 1 : 0,
                    ExistenciasPunto = p.PointStock ? 1 : 0,
                    PrediccionPunto = p.PointPrediction ? 1 : 0,
                    CustodiaPunto = p.PointCustody ? 1 : 0,

                    OtrosValoresPunto = p.PointOtherValues ? 1 : 0,
                    Otros = p.Others,
                    LiberacionEfectivoPunto = p.CashReleasePoint ? 1 : 0,

                    FondoPunto = p.PointFund ?? 0,
                    CodFondo = p.FundCode,
                    CodRutaSuc = p.RouteBranchCode,
                    CodRango = p.RangeCode,

                    FecIngreso = p.EntryDate ?? DateOnly.FromDateTime(DateTime.Today),
                    FecRetiro = p.WithdrawalDate,

                    CodCas4u = p.Cas4uCode,
                    NivelRiesgo = p.RiskLevel ?? "M",

                    CoberturaPunto = p.PointCoverage ?? "U",
                    EscalaInterurbanos = p.InterurbanScale ?? 0,

                    Consignacion = p.Consignment ?? 0,

                    EstadoPunto = p.Status,
                    CartaFilePath = p.RangeAttentionInfo
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Obtiene los datos para la vista previa de un punto.
        /// </summary>
        /// <param name="pointCode">Punto</param>
        /// <returns>Datos del punto para vista previa.</returns>
        public async Task<PointPreviewDto?> GetPreviewAsync(string pointCode)
        {
            return await _db.AdmPuntos
                .AsNoTracking()
                .Include(p => p.Client)
                .Include(p => p.Branch)
                .Include(p => p.City)
                .Include(p => p.Fund)
                .Include(p => p.Route)
                .Include(p => p.Range)
                .Where(p => p.PointCode == pointCode)
                .Select(p => new PointPreviewDto
                {
                    CodPunto = p.PointCode,
                    CodPCliente = p.ClientPointCode!,

                    ClienteNombre = p.Client!.ClientName,
                    ClientePpalNombre = p.MainClientCode == null ? null : p.MainClientCode.ToString(),

                    NombrePunto = p.PointName,
                    NombreCorto = p.ShortName,

                    NombreSucursal = p.Branch!.NombreSucursal,
                    NombreCiudad = p.City!.NombreCiudad,

                    EstadoPunto = p.Status,
                    FondoAsociado = p.Fund != null ? p.Fund.FundName : null,
                    RutaAsociada = p.Route != null ? p.Route.RouteName : null,
                    RangoAsociado = p.Range != null ? p.Range.RangeInformation : null,

                    NivelRiesgo = p.RiskLevel!,
                    CoberturaPunto = p.PointCoverage!,

                    FecIngreso = p.EntryDate ?? DateOnly.FromDateTime(DateTime.Today),
                    FecRetiro = p.WithdrawalDate,

                    LatPunto = p.Latitude,
                    LngPunto = p.Longitude,
                    RadioPunto = p.PointRadius != null ? int.Parse(p.PointRadius) : null,

                    BaseCambio = p.ChangeBase ? 1 : 0,
                    LlavesPunto = p.PointKeys ? 1 : 0,
                    DocumentosPunto = p.PointDocuments ? 1 : 0,
                    ExistenciasPunto = p.PointStock ? 1 : 0,
                    PrediccionPunto = p.PointPrediction ? 1 : 0,
                    CustodiaPunto = p.PointCustody ? 1 : 0,
                    OtrosValoresPunto = p.PointOtherValues ? 1 : 0,
                    LiberacionEfectivoPunto = p.CashReleasePoint ? 1 : 0,

                    Otros = p.Others,
                    CartaFilePath = p.RangeAttentionInfo
                })
                .FirstOrDefaultAsync();
        }
    }
}
