using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models.Entities;
using VCashApp.Services.Employee.Domain;

namespace VCashApp.Services.Employee.Infrastructure
{
    /// <summary>
    /// Proporciona una implementación de Entity Framework para el repositorio de empleados.
    /// </summary>
    public class EfEmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _db;
        private readonly IBranchContext _branchCtx;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="EfEmployeeRepository"/>.
        /// </summary>
        /// <param name="db">Contexto de la base de datos.</param>
        /// <param name="branchCtx">Contexto de sucursal.</param>
        public EfEmployeeRepository(AppDbContext db, IBranchContext branchCtx)
        {
            _db = db;
            _branchCtx = branchCtx;
        }

        /// <summary>
        /// Busca empleados según los parámetros proporcionados de forma asincrónica.
        /// </summary>
        /// <param name="cargoId">Código del cargo del empleado.</param>
        /// <param name="branchId">Codigo de la sucursal del empleado.</param>
        /// <param name="employeeStatus">Estado del empleado.</param>
        /// <param name="search">Busqueda de texto libre.</param>
        /// <param name="gender">Género del empleado.</param>
        /// <param name="page">Página actual para la paginación.</param>
        /// <param name="pageSize">Parámetro de tamaño de página para la paginación.</param>
        /// <param name="allBranches">Indica si se deben incluir todas las sucursales.</param>
        /// <param name="currentBranchId">Id de la sucursal actual (si aplica).</param>
        /// <param name="permittedBranches">Lista de sucursales permitidas (si aplica).</param>
        /// <returns>Una tarea que representa la operación asincrónica, con una tupla que contiene los empleados encontrados y el total.</returns>
        public async Task<(IEnumerable<AdmEmpleado> Items, int Total)> SearchAsync(
            int? cargoId, int? branchId, int? employeeStatus,
            string? search, string? gender, int page, int pageSize,
            bool allBranches, int? currentBranchId, IEnumerable<int> permittedBranches)
        {
            var q = _db.AdmEmpleados
                .Include(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(e => e.Sucursal).AsNoTracking().AsQueryable();

            if (branchId.HasValue) q = q.Where(e => e.CodSucursal == branchId.Value);
            else
            {
                if (!allBranches && currentBranchId.HasValue)
                    q = q.Where(e => e.CodSucursal == currentBranchId.Value);
                else if (permittedBranches?.Any() == true)
                    q = q.Where(e => e.CodSucursal.HasValue && permittedBranches.Contains(e.CodSucursal.Value));
            }

            if (cargoId.HasValue) q = q.Where(e => e.CodCargo == cargoId.Value);
            if (employeeStatus.HasValue) q = q.Where(e => (int?)e.EmpleadoEstado == employeeStatus.Value);
            if (!string.IsNullOrWhiteSpace(gender)) q = q.Where(e => e.Genero == gender);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var words = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                q = q.Where(e =>
                    words.Any(w =>
                        (e.NombreCompleto ?? "").Contains(w) ||
                        (e.PrimerNombre ?? "").Contains(w) ||
                        (e.PrimerApellido ?? "").Contains(w) ||
                        (e.SegundoNombre ?? "").Contains(w) ||
                        (e.SegundoApellido ?? "").Contains(w) ||
                        (e.NumeroCarnet ?? "").Contains(w)
                    )
                );
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(c => c.FecVinculacion).ThenBy(e => e.SegundoApellido).ThenBy(e => e.PrimerNombre)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        /// <summary>
        /// Obtiene un empleado por su ID de cédula de forma asincrónica.
        /// </summary>
        /// <param name="codCedula">Documento de identidad del empleado.</param>
        /// <returns>Una tarea que representa la operación asincrónica, con el empleado si se encuentra, o null en caso contrario.</returns>
        public Task<AdmEmpleado?> GetByIdAsync(int codCedula)
            =>  _db.AdmEmpleados
                .Include(e => e.Cargo).ThenInclude(c => c.Unidad)
                .Include(e => e.Sucursal)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.CodCedula == codCedula);

        /// <summary>
        /// Asincronamente agrega un nuevo empleado al contexto de la base de datos.
        /// </summary>
        /// <param name="entity">El empleado a agregar.</param>
        /// <returns>Una tarea que representa la operación asincrónica.</returns>
        public async Task AddAsync(AdmEmpleado entity)
        {
            await _db.AdmEmpleados.AddAsync(entity);
        }

        /// <summary>
        /// Actualiza un empleado existente.
        /// </summary>
        /// <param name="entity">El empleado a actualizar.</param>
        /// <returns>Una tarea que representa la operación asincrónica.</returns>
        public Task UpdateAsync(AdmEmpleado entity)
        {
            _db.AdmEmpleados.Update(entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Guarda los cambios pendientes en el contexto de la base de datos.
        /// </summary>
        /// <returns>Una tarea que representa la operación asincrónica.</returns>
        public Task SaveChangesAsync() => _db.SaveChangesAsync();

        /// <summary>
        /// Obtiene la lista de cargos.
        /// </summary>
        /// <returns>Una lista de tuplas que contienen el valor y el texto de cada cargo.</returns>
        public async Task<IEnumerable<(int Value, string Text)>> GetCargosAsync()
            => await _db.AdmCargos
                .OrderBy(c => c.CodCargo)
                .Select(c => new ValueTuple<int, string>(c.CodCargo, c.CodCargo + " - " + (c.NombreCargo ?? "")))
                .ToListAsync();

        /// <summary>
        /// Obtiene la lista de sucursales según los parámetros proporcionados.
        /// </summary>
        /// <param name="allBranches">Indica si se deben incluir todas las sucursales.</param>
        /// <param name="currentBranchId">Id de la sucursal actual (si aplica).</param>
        /// <param name="permittedBranches">Lista de sucursales permitidas (si aplica).</param>
        /// <returns>Una lista de tuplas que contienen el valor y el texto de cada sucursal.</returns>
        public async Task<IEnumerable<(int Value, string Text)>> GetSucursalesAsync(
            bool allBranches, int? currentBranchId, IEnumerable<int> permittedBranches)
        {
            var q = _db.AdmSucursales.AsNoTracking().AsQueryable();
            if (!allBranches && currentBranchId.HasValue)
                q = q.Where(s => s.CodSucursal == currentBranchId.Value);
            else if (permittedBranches?.Any() == true)
                q = q.Where(s => permittedBranches.Contains(s.CodSucursal));

            return await q.OrderBy(s => s.CodSucursal)
                          .Select(s => new ValueTuple<int, string>(s.CodSucursal, s.CodSucursal + " - " + (s.NombreSucursal ?? "")))
                          .ToListAsync();
        }

        /// <summary>
        /// Obtiene la lista de ciudades.
        /// </summary>
        /// <returns>Una lista de tuplas que contienen el valor y el texto de cada ciudad.</returns>
        public async Task<IEnumerable<(int Value, string Text)>> GetCiudadesAsync()
            => await _db.AdmCiudades
                .OrderBy(c => c.CodCiudad)
                .Select(c => new ValueTuple<int, string>(c.CodCiudad, c.CodCiudad + " - " + (c.NombreCiudad ?? "")))
                .ToListAsync();
    }
}
