using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VCashApp.Models;
using VCashApp.Models.Entities; 
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;

namespace VCashApp.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<PermisoPerfil> PermisosPerfil { get; set; }
        public DbSet<AdmVista> AdmVistas { get; set; }
        public DbSet<AdmDenominacion> AdmDenominaciones { get; set; }
        public DbSet<AdmUnidad> AdmUnidades { get; set; }
        public DbSet<AdmCargo> AdmCargos { get; set; }
        public DbSet<AdmPais> AdmPaises { get; set; }
        public DbSet<AdmDepartamento> AdmDepartamentos { get; set; }
        public DbSet<AdmCiudad> AdmCiudades { get; set; }
        public DbSet<AdmEmpleado> AdmEmpleados { get; set; }
        public DbSet<AdmVehiculo> AdmVehiculos { get; set; }
        public DbSet<AdmSucursal> AdmSucursales { get; set; }
        public DbSet<AdmRuta> AdmRutas { get; set; }
        public DbSet<SegRegistroEmpleado> SegRegistroEmpleados { get; set; }
        public DbSet<AdmEstado> AdmEstados { get; set; }
        public DbSet<AdmConcepto> AdmConceptos { get; set; }
        public DbSet<AdmConsecutivo> AdmConsecutivos { get; set; }
        public DbSet<AdmServicio> AdmServicios { get; set; }
        public DbSet<TdvRutaDiaria> TdvRutasDiarias { get; set; }
        // public DbSet<TdvRutaDetallePunto> TdvRutaDetallePuntos { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.NombreUsuario).HasColumnName("NombreUsuario").HasColumnType("VARCHAR(50)");
                entity.HasIndex(e => e.NombreUsuario).IsUnique();
            });

            // Mapeo para PermisoPerfil
            builder.Entity<PermisoPerfil>(entity => {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedOnAdd(); // Id autoincremental

                entity.Property(p => p.CodPerfilId).IsRequired().HasMaxLength(450); // FK a AspNetRoles.Id (string GUID)
                entity.Property(p => p.CodVista).IsRequired().HasMaxLength(50); // FK a AdmVista.CodVista

                // Relación con AspNetRoles (el perfil de Identity)
                entity.HasOne<IdentityRole>() // Apunta a IdentityRole
                      .WithMany()
                      .HasForeignKey(p => p.CodPerfilId) // La FK en PermisoPerfil
                      .HasPrincipalKey(r => r.Id) // La PK en AspNetRoles
                      .OnDelete(DeleteBehavior.Restrict); // No borrar en cascada

                // Relación con AdmVista
                entity.HasOne(p => p.Vista).WithMany().HasForeignKey(p => p.CodVista).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            });

            // Mapeo para AdmVista
            builder.Entity<AdmVista>(entity => {
                entity.HasKey(v => v.CodVista);
                entity.Property(v => v.CodVista).IsRequired().HasMaxLength(50); // Ajusta la longitud si es necesario
                entity.Property(v => v.NombreVista).HasMaxLength(100);
                entity.Property(v => v.RolAsociado).HasMaxLength(50); // El campo 'rol' que describes
            });

            builder.Entity<AdmUnidad>(entity => {
                entity.HasKey(u => u.CodUnidad);
                entity.Property(u => u.CodUnidad).IsRequired();
            });

            builder.Entity<AdmCargo>(entity => {
                entity.HasKey(c => c.CodCargo);
                entity.Property(c => c.CodCargo).IsRequired();
                entity.Property(c => c.Jornada).HasColumnType("TIME(0)");
                entity.Property(c => c.TiempoBreak).HasColumnType("TIME(0)");
                entity.HasOne(c => c.Unidad).WithMany().HasForeignKey(c => c.CodUnidad).IsRequired(false); // FK
            });

            builder.Entity<AdmPais>(entity => {
                entity.HasKey(p => p.CodPais);
                entity.Property(p => p.CodPais).IsRequired();
            });

            builder.Entity<AdmDepartamento>(entity => {
                entity.HasKey(d => d.CodDepartamento);
                entity.Property(d => d.CodDepartamento).IsRequired();
                entity.HasOne(d => d.Pais).WithMany().HasForeignKey(d => d.CodPais).IsRequired(false);
            });

            builder.Entity<AdmCiudad>(entity => {
                entity.HasKey(c => c.CodCiudad);
                entity.Property(c => c.CodCiudad).IsRequired();
                entity.HasOne(c => c.Departamento).WithMany().HasForeignKey(c => c.CodDepartamento).IsRequired(false);
                entity.HasOne(c => c.Pais).WithMany().HasForeignKey(c => c.CodPais).IsRequired(false);
                entity.HasOne(c => c.Sucursal).WithMany().HasForeignKey(c => c.CodSucursal).IsRequired(false);
            });

            builder.Entity<AdmSucursal>(entity => {
                entity.HasKey(s => s.CodSucursal);
                entity.Property(s => s.CodSucursal).IsRequired();
            });

            builder.Entity<AdmEmpleado>(entity => {
                entity.Property(e => e.CodCedula).IsRequired().ValueGeneratedNever();
                entity.HasOne(e => e.Cargo).WithMany().HasForeignKey(e => e.CodCargo).IsRequired(false);
                entity.HasOne(e => e.Sucursal).WithMany().HasForeignKey(e => e.CodSucursal).IsRequired(false);
            });

            builder.Entity<AdmVehiculo>(entity => {
                entity.Property(v => v.CodVehiculo).IsRequired();
                entity.Property(v => v.MontoAutorizado).HasColumnType("DECIMAL(18,0)");
                entity.Property(v => v.Toneladas).HasColumnType("DECIMAL(18,2)");
                entity.Property(v => v.VencimientoSoat).HasColumnType("DATE");
                entity.Property(v => v.VencimientoTecnomecanica).HasColumnType("DATE");
                entity.HasOne(v => v.Sucursal).WithMany().HasForeignKey(v => v.CodSucursal).IsRequired(false);
                entity.HasOne(v => v.Unidad).WithMany().HasForeignKey(v => v.CodUnidad).IsRequired(false);
                entity.HasOne(v => v.Conductor).WithMany().HasForeignKey(v => v.ConductorCedula).IsRequired(false);
            });

            builder.Entity<AdmRuta>(entity => {
                entity.HasKey(r => r.CodRutaSuc);
                entity.Property(r => r.CodRutaSuc).IsRequired();
                entity.Property(r => r.Monto).HasColumnType("DECIMAL(18,0)");
                entity.Property(r => r.LunesHoraInicio).HasColumnType("TIME(0)");
                entity.Property(r => r.LunesHoraFin).HasColumnType("TIME(0)");
                entity.Property(r => r.MartesHoraInicio).HasColumnType("TIME(0)");
                entity.Property(r => r.MartesHoraFin).HasColumnType("TIME(0)");
                entity.Property(r => r.MiercolesHoraInicio).HasColumnType("TIME(0)");
                entity.Property(r => r.MiercolesHoraFin).HasColumnType("TIME(0)");
                entity.Property(r => r.JuevesHoraInicio).HasColumnType("TIME(0)");
                entity.Property(r => r.JuevesHoraFin).HasColumnType("TIME(0)");
                entity.Property(r => r.ViernesHoraInicio).HasColumnType("TIME(0)");
                entity.Property(r => r.ViernesHoraFin).HasColumnType("TIME(0)");
                entity.Property(r => r.SabadoHoraInicio).HasColumnType("TIME(0)");
                entity.Property(r => r.SabadoHoraFin).HasColumnType("TIME(0)");
                entity.Property(r => r.DomingoHoraInicio).HasColumnType("TIME(0)");
                entity.Property(r => r.DomingoHoraFin).HasColumnType("TIME(0)");
                entity.Property(r => r.FestivoHoraInicio).HasColumnType("TIME(0)");
                entity.Property(r => r.FestivoHoraFin).HasColumnType("TIME(0)");
                entity.HasOne(r => r.Sucursal).WithMany().HasForeignKey(r => r.CodSucursal).IsRequired(false);
            });

            builder.Entity<SegRegistroEmpleado>(entity =>
            {
                entity.HasKey(re => re.Id);
                entity.Property(re => re.Id).ValueGeneratedOnAdd();
                entity.Property(re => re.CodCedula).IsRequired();
                entity.Property(re => re.CodCargo).IsRequired();
                entity.Property(re => re.CodUnidad).IsRequired().HasMaxLength(450);
                entity.Property(re => re.CodSucursal).IsRequired();

                entity.Property(re => re.FechaEntrada).IsRequired().HasColumnType("DATE");
                entity.Property(re => re.HoraEntrada).IsRequired().HasColumnType("TIME(0)");
                entity.Property(re => re.FechaSalida).HasColumnType("DATE");
                entity.Property(re => re.HoraSalida).HasColumnType("TIME(0)");

                entity.Property(re => re.IndicadorEntrada).IsRequired();
                entity.Property(re => re.IndicadorSalida).IsRequired();

                entity.Property(re => re.RegistroUsuarioId).IsRequired().HasMaxLength(450);

                entity.HasOne(re => re.Empleado).WithMany().HasForeignKey(re => re.CodCedula).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(re => re.Cargo).WithMany().HasForeignKey(re => re.CodCargo).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(re => re.Unidad).WithMany().HasForeignKey(re => re.CodUnidad).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(re => re.Sucursal).WithMany().HasForeignKey(re => re.CodSucursal).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(re => re.UsuarioRegistro).WithMany().HasForeignKey(re => re.RegistroUsuarioId).IsRequired(false).OnDelete(DeleteBehavior.Restrict); // Nullable en DB
            });

            builder.Entity<AdmEstado>(entity => {
                entity.HasKey(e => e.CodEstado);
                entity.Property(e => e.CodEstado).IsRequired();
            });

            builder.Entity<AdmConcepto>(entity => {
                entity.HasKey(c => c.CodConcepto);
                entity.Property(c => c.CodConcepto).IsRequired();
            });

            builder.Entity<AdmConsecutivo>(entity => {
                entity.HasKey(c => c.TipoConsecutivo);
                entity.Property(c => c.TipoConsecutivo).IsRequired();
            });

            builder.Entity<AdmDenominacion>(entity => {
                entity.HasKey(d => d.CodDenominacion);
                entity.Property(d => d.CodDenominacion).IsRequired();
                entity.Property(d => d.ValorDenominacion).HasColumnType("DECIMAL(18,0)");
            });

            builder.Entity<AdmServicio>(entity => {
                entity.HasKey(s => s.OrdenServicio);
                entity.Property(s => s.OrdenServicio).IsRequired();
                entity.Property(s => s.FechaSolicitud).HasColumnType("DATE"); // Tipo SQL explícito
                entity.Property(s => s.HoraSolicitud).HasColumnType("TIME(0)"); // Tipo SQL explícito
                entity.Property(s => s.FechaAceptacion).HasColumnType("DATE");
                entity.Property(s => s.HoraAceptacion).HasColumnType("TIME(0)");
                entity.Property(s => s.FechaProgramacion).HasColumnType("DATE");
                entity.Property(s => s.HoraProgramacion).HasColumnType("TIME(0)");
                entity.Property(s => s.FechaAtencionInicial).HasColumnType("DATE");
                entity.Property(s => s.HoraAtencionInicial).HasColumnType("TIME(0)");
                entity.Property(s => s.FechaAtencionFinal).HasColumnType("DATE");
                entity.Property(s => s.HoraAtencionFinal).HasColumnType("TIME(0)");
                entity.Property(s => s.FechaCancelacion).HasColumnType("DATE");
                entity.Property(s => s.HoraCancelacion).HasColumnType("TIME(0)");
                entity.Property(s => s.FechaRechazo).HasColumnType("DATE");
                entity.Property(s => s.HoraRechazo).HasColumnType("TIME(0)");
                entity.Property(s => s.ValorServicio).HasColumnType("DECIMAL(18,0)");
                entity.Property(s => s.ValorBillete).HasColumnType("DECIMAL(18,0)");
                entity.Property(s => s.ValorMoneda).HasColumnType("DECIMAL(18,0)");

                //entity.HasOne(s => s.Cliente).WithMany().HasForeignKey(s => s.CodCliente).IsRequired(false);
                entity.HasOne(s => s.Sucursal).WithMany().HasForeignKey(s => s.CodSucursal).IsRequired(false);
                entity.HasOne(s => s.Concepto).WithMany().HasForeignKey(s => s.CodConcepto).IsRequired(false);
                entity.HasOne(s => s.Estado).WithMany().HasForeignKey(s => s.CodEstado).IsRequired(false);
            });

            // Mapeo para TdvRutasDiarias (definido en Models/Entities/TdvRutasDiarias.cs)
            builder.Entity<TdvRutaDiaria>(entity => {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnType("VARCHAR(12)").IsRequired(); // Id es requerido

                entity.Property(e => e.KmInicial).HasColumnType("NUMERIC(18,0)");
                entity.Property(e => e.KmFinal).HasColumnType("NUMERIC(18,0)");
                entity.Property(e => e.FechaIngresoJT).HasColumnType("DATE");
                entity.Property(e => e.HoraIngresoJT).HasColumnType("TIME(0)");
                entity.Property(e => e.FechaSalidaJT).HasColumnType("DATE");
                entity.Property(e => e.HoraSalidaJT).HasColumnType("TIME(0)");

                // Definir las relaciones FK explícitamente (algunas pueden inferirse)
                entity.HasOne(e => e.RutaMaster).WithMany().HasForeignKey(e => e.CodRutaSuc).HasPrincipalKey(r => r.CodRutaSuc).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Sucursal).WithMany().HasForeignKey(e => e.CodSucursal).HasPrincipalKey(s => s.CodSucursal).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Vehiculo).WithMany().HasForeignKey(e => e.CodVehiculo).HasPrincipalKey(v => v.CodVehiculo).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.JT).WithMany().HasForeignKey(e => e.CedulaJT).HasPrincipalKey(emp => emp.CodCedula).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Conductor).WithMany().HasForeignKey(e => e.CedulaConductor).HasPrincipalKey(emp => emp.CodCedula).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Tripulante).WithMany().HasForeignKey(e => e.CedulaTripulante).HasPrincipalKey(emp => emp.CodCedula).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CargoJTObj).WithMany().HasForeignKey(e => e.CodCargoJT).HasPrincipalKey(c => c.CodCargo).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CargoConductorObj).WithMany().HasForeignKey(e => e.CodCargoConductor).HasPrincipalKey(c => c.CodCargo).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CargoTripulanteObj).WithMany().HasForeignKey(e => e.CodCargoTripulante).HasPrincipalKey(c => c.CodCargo).OnDelete(DeleteBehavior.Restrict);

                // FKs a ApplicationUser (Id es string GUID)
                entity.HasOne(e => e.UsuarioPlaneacionObj).WithMany().HasForeignKey(e => e.UsuarioPlaneacion).HasPrincipalKey(u => u.Id).IsRequired(false);
                entity.HasOne(e => e.UsuarioCEFCargueObj).WithMany().HasForeignKey(e => e.UsuarioCEFCargue).HasPrincipalKey(u => u.Id).IsRequired(false);
                entity.HasOne(e => e.UsuarioCEFDescargueObj).WithMany().HasForeignKey(e => e.UsuarioCEFDescargue).HasPrincipalKey(u => u.Id).IsRequired(false);
                entity.HasOne(e => e.UsuarioSupervisorAperturaObj).WithMany().HasForeignKey(e => e.UsuarioSupervisorApertura).HasPrincipalKey(u => u.Id).IsRequired(false);
                entity.HasOne(e => e.UsuarioSupervisorCierreObj).WithMany().HasForeignKey(e => e.UsuarioSupervisorCierre).HasPrincipalKey(u => u.Id).IsRequired(false);
            });

            // Mapeo para TdvRutaDetallePuntos
            /*builder.Entity<TdvRutaDetallePunto>(entity => {
                entity.HasKey(e => e.IdDetallePunto);
                entity.Property(e => e.IdDetallePunto).ValueGeneratedOnAdd();

                entity.HasOne(e => e.RutaDiaria).WithMany(r => r.DetallePuntos).HasForeignKey(e => e.IdRutaDiaria).IsRequired(false);
                entity.HasOne(e => e.OrigenDetalle).WithMany().HasForeignKey(e => e.Origen).IsRequired(false);
                entity.HasOne(e => e.DestinoDetalle).WithMany().HasForeignKey(e => e.Destino).IsRequired(false);
                entity.HasOne(e => e.Servicio).WithMany().HasForeignKey(e => e.IdServicio).IsRequired(false);
                entity.HasOne(e => e.SucursalPunto).WithMany().HasForeignKey(e => e.CodSucursal).IsRequired(false);
                entity.HasOne(e => e.UsuarioAtencionObj).WithMany().HasForeignKey(e => e.UsuarioAtencion).IsRequired(false);
            });*/ 
        }

        public async Task<Tuple<List<SegRegistroEmpleado>, int>> GetFilteredEmployeeLogsFromSpAsync(
            string userId,
            List<int> permittedBranchIds,
            int? cargoId,
            string? unitId,
            int? branchIdFilter,
            DateOnly? startDate,
            DateOnly? endDate,
            int? logStatus,
            string? search,
            int page,
            int pageSize,
            bool isAdmin)
        {
            DataTable tvpTable = new DataTable();
            tvpTable.Columns.Add("Value", typeof(int));
            foreach (int id in permittedBranchIds)
            {
                tvpTable.Rows.Add(id);
            }

            var pUserId = new SqlParameter("@UserId", userId);
            var pPermittedBranchIds = new SqlParameter("@PermittedBranchIds", tvpTable)
            {
                TypeName = "dbo.IntListType",
                SqlDbType = SqlDbType.Structured
            };
            if (!permittedBranchIds.Any())
            {
                pPermittedBranchIds.Value = DBNull.Value;
            }

            var pCargoId = new SqlParameter("@CargoId", cargoId ?? (object)DBNull.Value);
            var pUnitId = new SqlParameter("@UnitId", string.IsNullOrEmpty(unitId) ? (object)DBNull.Value : unitId);
            var pBranchIdFilter = new SqlParameter("@BranchIdFilter", branchIdFilter ?? (object)DBNull.Value);
            var pStartDate = new SqlParameter("@StartDate", startDate.HasValue ? (object)startDate.Value.ToDateTime(TimeOnly.MinValue) : (object)DBNull.Value);
            var pEndDate = new SqlParameter("@EndDate", endDate.HasValue ? (object)endDate.Value.ToDateTime(TimeOnly.MaxValue) : (object)DBNull.Value);
            var pLogStatus = new SqlParameter("@LogStatus", logStatus ?? (object)DBNull.Value);
            var pSearchTerm = new SqlParameter("@SearchTerm", string.IsNullOrEmpty(search) ? (object)DBNull.Value : search);
            var pPage = new SqlParameter("@Page", page);
            var pPageSize = new SqlParameter("@PageSize", pageSize);
            var pIsAdmin = new SqlParameter("@IsAdmin", isAdmin);

            using (var command = Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "EXEC dbo.GetFilteredEmployeeLogs @UserId, @PermittedBranchIds, @CargoId, @UnitId, @BranchIdFilter, @StartDate, @EndDate, @LogStatus, @SearchTerm, @Page, @PageSize, @IsAdmin";
                command.Parameters.Add(pUserId);
                command.Parameters.Add(pPermittedBranchIds);
                command.Parameters.Add(pCargoId);
                command.Parameters.Add(pUnitId);
                command.Parameters.Add(pBranchIdFilter);
                command.Parameters.Add(pStartDate);
                command.Parameters.Add(pEndDate);
                command.Parameters.Add(pLogStatus);
                command.Parameters.Add(pSearchTerm);
                command.Parameters.Add(pPage);
                command.Parameters.Add(pPageSize);
                command.Parameters.Add(pIsAdmin);

                Database.OpenConnection();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    int totalCount = 0;
                    if (await reader.ReadAsync())
                    {
                        totalCount = reader.GetInt32(0);
                    }

                    await reader.NextResultAsync();

                    var logs = new List<SegRegistroEmpleado>();
                    while (await reader.ReadAsync())
                    {
                        var log = new SegRegistroEmpleado
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            CodCedula = reader.GetInt32(reader.GetOrdinal("CodCedula")),
                            PrimerNombreEmpleado = reader.IsDBNull(reader.GetOrdinal("PrimerNombreEmpleado")) ? null : reader.GetString(reader.GetOrdinal("PrimerNombreEmpleado")),
                            SegundoNombreEmpleado = reader.IsDBNull(reader.GetOrdinal("SegundoNombreEmpleado")) ? null : reader.GetString(reader.GetOrdinal("SegundoNombreEmpleado")),
                            PrimerApellidoEmpleado = reader.IsDBNull(reader.GetOrdinal("PrimerApellidoEmpleado")) ? null : reader.GetString(reader.GetOrdinal("PrimerApellidoEmpleado")),
                            SegundoApellidoEmpleado = reader.IsDBNull(reader.GetOrdinal("SegundoApellidoEmpleado")) ? null : reader.GetString(reader.GetOrdinal("SegundoApellidoEmpleado")),
                            CodCargo = reader.IsDBNull(reader.GetOrdinal("CodCargo")) ? 0 : reader.GetInt32(reader.GetOrdinal("CodCargo")),
                            NombreCargoEmpleado = reader.IsDBNull(reader.GetOrdinal("NombreCargoEmpleado")) ? null : reader.GetString(reader.GetOrdinal("NombreCargoEmpleado")),
                            CodUnidad = reader.IsDBNull(reader.GetOrdinal("CodUnidad")) ? null : reader.GetString(reader.GetOrdinal("CodUnidad")),
                            NombreUnidadEmpleado = reader.IsDBNull(reader.GetOrdinal("NombreUnidadEmpleado")) ? null : reader.GetString(reader.GetOrdinal("NombreUnidadEmpleado")),
                            CodSucursal = reader.IsDBNull(reader.GetOrdinal("CodSucursal")) ? 0 : reader.GetInt32(reader.GetOrdinal("CodSucursal")), // Confirmo que CodSucursal en SegRegistroEmpleado es int (no anulable)
                            NombreSucursalEmpleado = reader.IsDBNull(reader.GetOrdinal("NombreSucursalEmpleado")) ? null : reader.GetString(reader.GetOrdinal("NombreSucursalEmpleado")),
                            FechaEntrada = reader.IsDBNull(reader.GetOrdinal("FechaEntrada")) ? DateOnly.MinValue : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("FechaEntrada"))),
                            HoraEntrada = reader.IsDBNull(reader.GetOrdinal("HoraEntrada")) ? TimeOnly.MinValue : TimeOnly.FromTimeSpan(reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("HoraEntrada"))),
                            FechaSalida = reader.IsDBNull(reader.GetOrdinal("FechaSalida")) ? (DateOnly?)null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("FechaSalida"))),
                            HoraSalida = reader.IsDBNull(reader.GetOrdinal("HoraSalida")) ? (TimeOnly?)null : TimeOnly.FromTimeSpan(reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("HoraSalida"))),
                            IndicadorEntrada = reader.IsDBNull(reader.GetOrdinal("IndicadorEntrada")) ? false : reader.GetBoolean(reader.GetOrdinal("IndicadorEntrada")),
                            IndicadorSalida = reader.IsDBNull(reader.GetOrdinal("IndicadorSalida")) ? false : reader.GetBoolean(reader.GetOrdinal("IndicadorSalida")),
                            RegistroUsuarioId = reader.IsDBNull(reader.GetOrdinal("RegistroUsuarioId")) ? null : reader.GetString(reader.GetOrdinal("RegistroUsuarioId")),
                        };

                        if (!reader.IsDBNull(reader.GetOrdinal("Empleado_CodCedula")))
                        {
                            log.Empleado = new AdmEmpleado
                            {
                                CodCedula = reader.GetInt32(reader.GetOrdinal("Empleado_CodCedula")),
                                PrimerNombre = reader.IsDBNull(reader.GetOrdinal("Empleado_PrimerNombre")) ? null : reader.GetString(reader.GetOrdinal("Empleado_PrimerNombre")),
                                SegundoNombre = reader.IsDBNull(reader.GetOrdinal("Empleado_SegundoNombre")) ? null : reader.GetString(reader.GetOrdinal("Empleado_SegundoNombre")),
                                PrimerApellido = reader.IsDBNull(reader.GetOrdinal("Empleado_PrimerApellido")) ? null : reader.GetString(reader.GetOrdinal("Empleado_PrimerApellido")),
                                SegundoApellido = reader.IsDBNull(reader.GetOrdinal("Empleado_SegundoApellido")) ? null : reader.GetString(reader.GetOrdinal("Empleado_SegundoApellido")),
                                FotoUrl = reader.IsDBNull(reader.GetOrdinal("Empleado_FotoUrl")) ? null : reader.GetString(reader.GetOrdinal("Empleado_FotoUrl")),
                                CodCargo = reader.IsDBNull(reader.GetOrdinal("Empleado_CodCargo")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("Empleado_CodCargo")),
                                CodSucursal = reader.IsDBNull(reader.GetOrdinal("Empleado_CodSucursal")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("Empleado_CodSucursal"))
                            };

                            if (!reader.IsDBNull(reader.GetOrdinal("Cargo_CodCargo")))
                            {
                                log.Empleado.Cargo = new AdmCargo
                                {
                                    CodCargo = reader.GetInt32(reader.GetOrdinal("Cargo_CodCargo")),
                                    NombreCargo = reader.IsDBNull(reader.GetOrdinal("Cargo_NombreCargo")) ? null : reader.GetString(reader.GetOrdinal("Cargo_NombreCargo")),
                                };
                                if (!reader.IsDBNull(reader.GetOrdinal("Unidad_CodUnidad")))
                                {
                                    log.Empleado.Cargo.Unidad = new AdmUnidad
                                    {
                                        CodUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_CodUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_CodUnidad")),
                                        NombreUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_NombreUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_NombreUnidad")),
                                        TipoUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_TipoUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_TipoUnidad"))
                                    };
                                }
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("Sucursal_CodSucursal")))
                            {
                                log.Sucursal = new AdmSucursal
                                {
                                    CodSucursal = reader.GetInt32(reader.GetOrdinal("Sucursal_CodSucursal")),
                                    NombreSucursal = reader.IsDBNull(reader.GetOrdinal("Sucursal_NombreSucursal")) ? null : reader.GetString(reader.GetOrdinal("Sucursal_NombreSucursal"))
                                };
                            }
                        }
                        logs.Add(log);
                    }
                    return Tuple.Create(logs, totalCount);
                }
            }
        }

        public async Task<List<AdmEmpleado>> GetEmployeeInfoFromSpAsync(
            string userId,
            List<int> permittedBranchIds,
            string? searchInput,
            bool isAdmin)
        {
            DataTable tvpTable = new DataTable();
            tvpTable.Columns.Add("Value", typeof(int));
            foreach (int id in permittedBranchIds)
            {
                tvpTable.Rows.Add(id);
            }

            var pUserId = new SqlParameter("@UserId", userId);
            var pPermittedBranchIds = new SqlParameter("@PermittedBranchIds", tvpTable)
            {
                TypeName = "dbo.IntListType",
                SqlDbType = SqlDbType.Structured
            };
            if (!permittedBranchIds.Any())
            {
                pPermittedBranchIds.Value = DBNull.Value;
            }

            var pSearchInput = new SqlParameter("@SearchInput", string.IsNullOrEmpty(searchInput) ? (object)DBNull.Value : searchInput);
            var pIsAdmin = new SqlParameter("@IsAdmin", isAdmin);

            var employees = new List<AdmEmpleado>();

            using (var command = Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "EXEC dbo.GetEmployeeInfoFiltered @UserId, @PermittedBranchIds, @SearchInput, @IsAdmin";
                command.Parameters.Add(pUserId);
                command.Parameters.Add(pPermittedBranchIds);
                command.Parameters.Add(pSearchInput);
                command.Parameters.Add(pIsAdmin);

                Database.OpenConnection();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var employee = new AdmEmpleado
                        {
                            CodCedula = reader.GetInt32(reader.GetOrdinal("CodCedula")),
                            PrimerNombre = reader.IsDBNull(reader.GetOrdinal("PrimerNombre")) ? null : reader.GetString(reader.GetOrdinal("PrimerNombre")),
                            SegundoNombre = reader.IsDBNull(reader.GetOrdinal("SegundoNombre")) ? null : reader.GetString(reader.GetOrdinal("SegundoNombre")),
                            PrimerApellido = reader.IsDBNull(reader.GetOrdinal("PrimerApellido")) ? null : reader.GetString(reader.GetOrdinal("PrimerApellido")),
                            SegundoApellido = reader.IsDBNull(reader.GetOrdinal("SegundoApellido")) ? null : reader.GetString(reader.GetOrdinal("SegundoApellido")),
                            NombreCompleto = reader.IsDBNull(reader.GetOrdinal("NombreCompleto")) ? null : reader.GetString(reader.GetOrdinal("NombreCompleto")),
                            FotoUrl = reader.IsDBNull(reader.GetOrdinal("FotoUrl")) ? null : reader.GetString(reader.GetOrdinal("FotoUrl")),

                            CodCargo = reader.IsDBNull(reader.GetOrdinal("CodCargo")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CodCargo")),
                            CodSucursal = reader.IsDBNull(reader.GetOrdinal("CodSucursal")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CodSucursal")),

                            Cargo = new AdmCargo
                            {
                                CodCargo = reader.IsDBNull(reader.GetOrdinal("Cargo_CodCargo"))
                                   ? 0
                                   : reader.GetInt32(reader.GetOrdinal("Cargo_CodCargo")),
                                NombreCargo = reader.IsDBNull(reader.GetOrdinal("Cargo_NombreCargo")) ? null : reader.GetString(reader.GetOrdinal("Cargo_NombreCargo")),
                                Unidad = new AdmUnidad
                                {
                                    CodUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_CodUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_CodUnidad")),
                                    NombreUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_NombreUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_NombreUnidad")),
                                    TipoUnidad = reader.IsDBNull(reader.GetOrdinal("Unidad_TipoUnidad")) ? null : reader.GetString(reader.GetOrdinal("Unidad_TipoUnidad"))
                                }
                            },
                            Sucursal = new AdmSucursal
                            {
                                CodSucursal = reader.IsDBNull(reader.GetOrdinal("Sucursal_CodSucursal"))
                                      ? 0
                                      : reader.GetInt32(reader.GetOrdinal("Sucursal_CodSucursal")),
                                NombreSucursal = reader.IsDBNull(reader.GetOrdinal("Sucursal_NombreSucursal")) ? null : reader.GetString(reader.GetOrdinal("Sucursal_NombreSucursal"))
                            }
                        };
                        employees.Add(employee);
                    }
                }
            }
            return employees;
        }
    }
}