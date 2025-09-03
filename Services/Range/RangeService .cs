using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Models.AdmEntities;
using VCashApp.Models.ViewModels.Range;

namespace VCashApp.Services.Range
{
    public class RangeService : IRangeService
    {
        private readonly AppDbContext _context;

        public RangeService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Devuelve una lista de clientes activos para usar en un dropdown.
        /// </summary>
        /// <remarks>Solo clientes con estado activo (<c>Status == true</c>) son incluidos en el
        /// resultado.</remarks>
        /// <param name="selected">El cliente pre-seleccionado en el selector. Pasa <see langword="null"/> a indicacar que no hay
        /// pre-seleccion.</param>
        /// <returns>Lista de clientes <see cref="SelectListItem"/> Lista de clientes.</returns>
        public async Task<List<SelectListItem>> GetClientsForDropdownAsync(int? selected = null)
        {
            return await _context.AdmClientes
                .Where(c => c.Status == true
                         || (selected.HasValue && c.ClientCode == selected.Value))
                .OrderBy(c => c.ClientName)
                .Select(c => new SelectListItem
                {
                    Value = c.ClientCode.ToString(),
                    Text = c.ClientName + (c.Status ? "" : " (inactivo)"),
                    Selected = selected.HasValue && c.ClientCode == selected.Value
                })
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un listado paginado de rangos, con filtros opcionales por término de búsqueda, cliente y estado activo.
        /// </summary>
        /// <param name="search">Filtro de busqueda</param>
        /// <param name="clientId">Filtro del cliente</param>
        /// <param name="rangeStatus">Filtro de estado</param>
        /// <param name="page">Paginado</param>
        /// <param name="pageSize">Cantidad por pagina</param>
        /// <returns>Lista de rangos</returns>
        public async Task<RangeDashboardViewModel> GetPagedAsync(string? search, int? clientId, bool? rangeStatus, int page, int pageSize)
        {
            var q = _context.Set<AdmRange>()
                .Include(r => r.Client)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(r => r.CodRange.Contains(search) || (r.RangeInformation ?? "").Contains(search));
            }
            if (clientId.HasValue) q = q.Where(r => r.ClientId == clientId.Value);
            if (rangeStatus.HasValue) q = q.Where(r => r.RangeStatus == rangeStatus.Value);

            var total = await q.CountAsync();
            var data = await q.OrderBy(r => r.CodRange)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .Select(r => new RangeSummaryViewModel
                              {
                                  Id = r.Id,
                                  CodRange = r.CodRange,
                                  ClientId = r.ClientId,
                                  ClientName = r.Client.ClientName,
                                  RangeInformation = r.RangeInformation,
                                  RangeStatus = r.RangeStatus,
                                  ActiveDays =
                                      (r.Monday ? "Lunes, " : "") +
                                      (r.Tuesday ? "Martes, " : "") +
                                      (r.Wednesday ? "Miercoles, " : "") +
                                      (r.Thursday ? "Jueves, " : "") +
                                      (r.Friday ? "Viernes, " : "") +
                                      (r.Saturday ? "Sabado, " : "") +
                                      (r.Sunday ? "Domingo, " : "") +
                                      (r.Holiday ? "Festivo, " : "")
                              })
                              .ToListAsync();

            var vm = new RangeDashboardViewModel
            {
                Ranges = data,
                CurrentPage = page,
                PageSize = pageSize,
                TotalData = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                AvailableClients = await GetClientsForDropdownAsync(),
                SearchTerm = search,
                CurrentClientId = clientId,
                RangeStatus = rangeStatus
            };
            return vm;
        }

        /// <summary>
        /// Prepara el formulario para crear rangos.
        /// </summary>
        /// <returns>Formulario de creacion</returns>
        public async Task<RangeFormViewModel> PrepareCreateAsync()
        {
            return new RangeFormViewModel
            {
                RangeStatus = true,
                AvailableClients = await GetClientsForDropdownAsync()
            };
        }

        /// <summary>
        /// Metodo para crear un nuevo rango en la base de datos.
        /// </summary>
        /// <returns>Inserta los rangos en la base de datos</returns>
        public async Task<(bool ok, string? message, int? id)> CreateAsync(RangeFormViewModel vm)
        {
            var entity = MapToEntity(vm, new AdmRange());

            _context.Add(entity);
            try
            {
                await _context.SaveChangesAsync();
                return (true, null, entity.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return (false, "Ya existe un rango idéntico para este cliente.", null);
            }
        }

        /// <summary>
        /// Prepara el formulario para editar un rango existente.
        /// </summary>
        /// <returns>Formulario de edicion</returns>
        public async Task<RangeFormViewModel?> PrepareEditAsync(int id)
        {
            var r = await _context.Set<AdmRange>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return null;

            var vm = MapToVm(r);
            vm.AvailableClients = await GetClientsForDropdownAsync(r.ClientId);
            return vm;
        }

        public async Task<(bool ok, string? message)> UpdateAsync(RangeFormViewModel vm)
        {
            var entity = await _context.Set<AdmRange>().FirstOrDefaultAsync(x => x.Id == vm.Id);
            if (entity == null) return (false, "Rango no encontrado.");

            MapToEntity(vm, entity);
            try
            {
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return (false, "Ya existe un rango idéntico para este cliente.");
            }
        }

        /// <summary>
        /// Método auxiliar para obtener entidad con todos los datos relacionados.
        /// </summary>
        public async Task<AdmRange?> GetByIdAsync(int id)
        {
            return await _context.Set<AdmRange>()
                .Include(x => x.Client)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(bool ok, string? message)> DeleteAsync(int id)
        {
            var r = await _context.Set<AdmRange>().FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return (false, "Rango no encontrado.");

            // Soft-delete: solo desactivar
            r.RangeStatus = false;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            // SQL Server: 2601 (duplicate key) / 2627 (unique index)
            var sqlEx = ex.InnerException as SqlException;
            return sqlEx != null && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }

        private static AdmRange MapToEntity(RangeFormViewModel vm, AdmRange e)
        {
            e.CodRange = vm.CodRange;
            e.ClientId = vm.ClientId;
            e.RangeInformation = vm.RangeInformation;

            e.Monday = vm.Monday; e.MondayId = vm.MondayId;
            e.Lr1Hi = vm.Lr1Hi; e.Lr1Hf = vm.Lr1Hf; e.Lr2Hi = vm.Lr2Hi; e.Lr2Hf = vm.Lr2Hf; e.Lr3Hi = vm.Lr3Hi; e.Lr3Hf = vm.Lr3Hf;

            e.Tuesday = vm.Tuesday; e.TuesdayId = vm.TuesdayId;
            e.Mr1Hi = vm.Mr1Hi; e.Mr1Hf = vm.Mr1Hf; e.Mr2Hi = vm.Mr2Hi; e.Mr2Hf = vm.Mr2Hf; e.Mr3Hi = vm.Mr3Hi; e.Mr3Hf = vm.Mr3Hf;

            e.Wednesday = vm.Wednesday; e.WednesdayId = vm.WednesdayId;
            e.Wr1Hi = vm.Wr1Hi; e.Wr1Hf = vm.Wr1Hf; e.Wr2Hi = vm.Wr2Hi; e.Wr2Hf = vm.Wr2Hf; e.Wr3Hi = vm.Wr3Hi; e.Wr3Hf = vm.Wr3Hf;

            e.Thursday = vm.Thursday; e.ThursdayId = vm.ThursdayId;
            e.Jr1Hi = vm.Jr1Hi; e.Jr1Hf = vm.Jr1Hf; e.Jr2Hi = vm.Jr2Hi; e.Jr2Hf = vm.Jr2Hf; e.Jr3Hi = vm.Jr3Hi; e.Jr3Hf = vm.Jr3Hf;

            e.Friday = vm.Friday; e.FridayId = vm.FridayId;
            e.Vr1Hi = vm.Vr1Hi; e.Vr1Hf = vm.Vr1Hf; e.Vr2Hi = vm.Vr2Hi; e.Vr2Hf = vm.Vr2Hf; e.Vr3Hi = vm.Vr3Hi; e.Vr3Hf = vm.Vr3Hf;

            e.Saturday = vm.Saturday; e.SaturdayId = vm.SaturdayId;
            e.Sr1Hi = vm.Sr1Hi; e.Sr1Hf = vm.Sr1Hf; e.Sr2Hi = vm.Sr2Hi; e.Sr2Hf = vm.Sr2Hf; e.Sr3Hi = vm.Sr3Hi; e.Sr3Hf = vm.Sr3Hf;

            e.Sunday = vm.Sunday; e.SundayId = vm.SundayId;
            e.Dr1Hi = vm.Dr1Hi; e.Dr1Hf = vm.Dr1Hf; e.Dr2Hi = vm.Dr2Hi; e.Dr2Hf = vm.Dr2Hf; e.Dr3Hi = vm.Dr3Hi; e.Dr3Hf = vm.Dr3Hf;

            e.Holiday = vm.Holiday; e.HolidayId = vm.HolidayId;
            e.Fr1Hi = vm.Fr1Hi; e.Fr1Hf = vm.Fr1Hf; e.Fr2Hi = vm.Fr2Hi; e.Fr2Hf = vm.Fr2Hf; e.Fr3Hi = vm.Fr3Hi; e.Fr3Hf = vm.Fr3Hf;

            e.RangeStatus = vm.RangeStatus;
            return e;
        }

        private static RangeFormViewModel MapToVm(AdmRange r)
        {
            return new RangeFormViewModel
            {
                Id = r.Id,
                CodRange = r.CodRange,
                ClientId = r.ClientId,
                RangeInformation = r.RangeInformation,

                Monday = r.Monday,
                MondayId = r.MondayId,
                Lr1Hi = r.Lr1Hi,
                Lr1Hf = r.Lr1Hf,
                Lr2Hi = r.Lr2Hi,
                Lr2Hf = r.Lr2Hf,
                Lr3Hi = r.Lr3Hi,
                Lr3Hf = r.Lr3Hf,

                Tuesday = r.Tuesday,
                TuesdayId = r.TuesdayId,
                Mr1Hi = r.Mr1Hi,
                Mr1Hf = r.Mr1Hf,
                Mr2Hi = r.Mr2Hi,
                Mr2Hf = r.Mr2Hf,
                Mr3Hi = r.Mr3Hi,
                Mr3Hf = r.Mr3Hf,

                Wednesday = r.Wednesday,
                WednesdayId = r.WednesdayId,
                Wr1Hi = r.Wr1Hi,
                Wr1Hf = r.Wr1Hf,
                Wr2Hi = r.Wr2Hi,
                Wr2Hf = r.Wr2Hf,
                Wr3Hi = r.Wr3Hi,
                Wr3Hf = r.Wr3Hf,

                Thursday = r.Thursday,
                ThursdayId = r.ThursdayId,
                Jr1Hi = r.Jr1Hi,
                Jr1Hf = r.Jr1Hf,
                Jr2Hi = r.Jr2Hi,
                Jr2Hf = r.Jr2Hf,
                Jr3Hi = r.Jr3Hi,
                Jr3Hf = r.Jr3Hf,

                Friday = r.Friday,
                FridayId = r.FridayId,
                Vr1Hi = r.Vr1Hi,
                Vr1Hf = r.Vr1Hf,
                Vr2Hi = r.Vr2Hi,
                Vr2Hf = r.Vr2Hf,
                Vr3Hi = r.Vr3Hi,
                Vr3Hf = r.Vr3Hf,

                Saturday = r.Saturday,
                SaturdayId = r.SaturdayId,
                Sr1Hi = r.Sr1Hi,
                Sr1Hf = r.Sr1Hf,
                Sr2Hi = r.Sr2Hi,
                Sr2Hf = r.Sr2Hf,
                Sr3Hi = r.Sr3Hi,
                Sr3Hf = r.Sr3Hf,

                Sunday = r.Sunday,
                SundayId = r.SundayId,
                Dr1Hi = r.Dr1Hi,
                Dr1Hf = r.Dr1Hf,
                Dr2Hi = r.Dr2Hi,
                Dr2Hf = r.Dr2Hf,
                Dr3Hi = r.Dr3Hi,
                Dr3Hf = r.Dr3Hf,

                Holiday = r.Holiday,
                HolidayId = r.HolidayId,
                Fr1Hi = r.Fr1Hi,
                Fr1Hf = r.Fr1Hf,
                Fr2Hi = r.Fr2Hi,
                Fr2Hf = r.Fr2Hf,
                Fr3Hi = r.Fr3Hi,
                Fr3Hf = r.Fr3Hf,

                RangeStatus = r.RangeStatus
            };
        }

        // ===========================
        //  Validación de unicidad
        // ===========================
        public async Task<(bool ok, string? message)> ValidateUniqueAsync(RangeFormViewModel vm)
        {
            if (vm == null) return (false, "Datos inválidos.");
            if (vm.ClientId <= 0) return (false, "Cliente inválido.");

            var key = BuildScheduleKey(vm);

            var exists = await _context.Set<AdmRange>()
                .AsNoTracking()
                .Where(r => r.ClientId == vm.ClientId
                         && r.RangeStatus == true
                         && EF.Property<string>(r, "schedule_key") == key
                         && (!vm.Id.HasValue || r.Id != vm.Id.Value))
                .AnyAsync();

            return exists
                ? (false, "Ya existe un rango idéntico para este cliente.")
                : (true, null);
        }

        // ===========================
        //  Helpers
        // ===========================
        private static string BuildScheduleKey(RangeFormViewModel vm)
        {
            static string F(TimeSpan? t) => t.HasValue ? t.Value.ToString(@"hh\:mm") : "00:00";
            static string D(bool flag, TimeSpan? a1, TimeSpan? b1, TimeSpan? a2, TimeSpan? b2, TimeSpan? a3, TimeSpan? b3)
                => (flag ? "1" : "0") + ":" +
                   F(a1) + "-" + F(b1) + "," +
                   F(a2) + "-" + F(b2) + "," +
                   F(a3) + "-" + F(b3);

            var body = string.Join("|", new[]
            {
                D(vm.Monday,    vm.Lr1Hi, vm.Lr1Hf, vm.Lr2Hi, vm.Lr2Hf, vm.Lr3Hi, vm.Lr3Hf),
                D(vm.Tuesday,   vm.Mr1Hi, vm.Mr1Hf, vm.Mr2Hi, vm.Mr2Hf, vm.Mr3Hi, vm.Mr3Hf),
                D(vm.Wednesday, vm.Wr1Hi, vm.Wr1Hf, vm.Wr2Hi, vm.Wr2Hf, vm.Wr3Hi, vm.Wr3Hf),
                D(vm.Thursday,  vm.Jr1Hi, vm.Jr1Hf, vm.Jr2Hi, vm.Jr2Hf, vm.Jr3Hi, vm.Jr3Hf),
                D(vm.Friday,    vm.Vr1Hi, vm.Vr1Hf, vm.Vr2Hi, vm.Vr2Hf, vm.Vr3Hi, vm.Vr3Hf),
                D(vm.Saturday,  vm.Sr1Hi, vm.Sr1Hf, vm.Sr2Hi, vm.Sr2Hf, vm.Sr3Hi, vm.Sr3Hf),
                D(vm.Sunday,    vm.Dr1Hi, vm.Dr1Hf, vm.Dr2Hi, vm.Dr2Hf, vm.Dr3Hi, vm.Dr3Hf),
                D(vm.Holiday,   vm.Fr1Hi, vm.Fr1Hf, vm.Fr2Hi, vm.Fr2Hf, vm.Fr3Hi, vm.Fr3Hf)
            });

            return $"{vm.ClientId}:{body}";
        }
    }
}
