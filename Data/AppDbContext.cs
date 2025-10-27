using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VCashApp.Models;
using VCashApp.Models.Entities; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using VCashApp.Models.AdmEntities;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        private readonly IBranchContext? _branchContext;
        public AppDbContext(DbContextOptions<AppDbContext> options, IBranchContext? branchContext = null)
            : base(options)
        {
            _branchContext = branchContext;
        }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<PermisoPerfil> PermisosPerfil { get; set; }
        public DbSet<AdmVista> AdmVistas { get; set; }
        public DbSet<AdmState> AdmEstados { get; set; }
        public DbSet<AdmDenominacion> AdmDenominaciones { get; set; }
        public DbSet<AdmUnidad> AdmUnidades { get; set; }
        public DbSet<AdmCargo> AdmCargos { get; set; }
        public DbSet<AdmPais> AdmPaises { get; set; }
        public DbSet<AdmDepartamento> AdmDepartamentos { get; set; }
        public DbSet<AdmCiudad> AdmCiudades { get; set; }
        public DbSet<AdmCliente> AdmClientes { get; set; }
        public DbSet<AdmEmpleado> AdmEmpleados { get; set; }
        public DbSet<AdmVehiculo> AdmVehiculos { get; set; }
        public DbSet<AdmSucursal> AdmSucursales { get; set; }
        public DbSet<AdmFondo> AdmFondos { get; set; }
        public DbSet<AdmPunto> AdmPuntos { get; set; }
        public DbSet<AdmRuta> AdmRutas { get; set; }
        public DbSet<AdmRange> AdmRangos { get; set; }
        public DbSet<SegRegistroEmpleado> SegRegistroEmpleados { get; set; }
        public DbSet<AdmConcepto> AdmConceptos { get; set; }
        public DbSet<AdmConsecutivo> AdmConsecutivos { get; set; }
        public DbSet<AdmBankEntiy> AdmBankEntities { get; set; }
        public DbSet<CgsService> CgsServicios { get; set; }
        public DbSet<CgsLocationType> CgsLocationTypes { get; set; }
        public DbSet<CefTransaction> CefTransactions { get; set; }
        public DbSet<CefContainer> CefContainers { get; set; }
        public DbSet<CefValueDetail> CefValueDetails { get; set; }
        public DbSet<CefIncident> CefIncidents { get; set; }
        public DbSet<CefIncidentType> CefIncidentTypes { get; set; }
        public DbSet<AdmQuality> AdmCalidad { get; set; }
        public DbSet<TdvRutaDiaria> TdvRutasDiarias { get; set; }
        // public DbSet<TdvRutaDetallePunto> TdvRutaDetallePuntos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.NombreUsuario).HasColumnName("NombreUsuario").HasColumnType("VARCHAR(50)");
                entity.HasIndex(e => e.NombreUsuario).IsUnique();

                entity.HasMany(u => u.Claims)
                      .WithOne()
                      .HasForeignKey(uc => uc.UserId)
                      .IsRequired();

                entity.HasMany(u => u.UserRoles)
                      .WithOne()
                      .HasForeignKey(ur => ur.UserId)
                      .IsRequired();
            });

            builder.Entity<PermisoPerfil>(entity => {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedOnAdd();

                entity.Property(p => p.CodPerfilId).IsRequired().HasMaxLength(450);
                entity.Property(p => p.CodVista).IsRequired().HasMaxLength(50);

                entity.HasOne<IdentityRole>()
                      .WithMany()
                      .HasForeignKey(p => p.CodPerfilId)
                      .HasPrincipalKey(r => r.Id)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Vista).WithMany().HasForeignKey(p => p.CodVista).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AdmVista>(entity => {
                entity.HasKey(v => v.CodVista);
                entity.Property(v => v.CodVista).IsRequired().HasMaxLength(50); // Ajusta la longitud si es necesario
                entity.Property(v => v.NombreVista).HasMaxLength(100);
                entity.Property(v => v.RolAsociado).HasMaxLength(50); // El campo 'rol' que describes
            });

            builder.Entity<AdmState>(entity => {
                entity.HasKey(s => s.StateCode);
                entity.Property(s => s.StateCode).IsRequired();
                entity.Property(s => s.StateCode).ValueGeneratedNever();

                entity.Property(s => s.StateName).IsRequired().HasMaxLength(100);
            });

            builder.Entity<AdmCliente>(entity =>{
                entity.ToTable("AdmClientes");
                entity.HasKey(c => c.ClientCode);
                entity.Property(c => c.ClientCode).ValueGeneratedOnAdd();

                entity.Property(c => c.ClientName).IsRequired().HasMaxLength(255);
                entity.Property(c => c.BusinessName).IsRequired().HasMaxLength(255);
                entity.Property(c => c.ClientAcronym).IsRequired().HasMaxLength(5);
                entity.Property(c => c.DocumentType).IsRequired();
                entity.Property(c => c.DocumentNumber).IsRequired();
                entity.Property(c => c.Contact1).HasMaxLength(255);
                entity.Property(c => c.PositionContact1).HasMaxLength(255);
                entity.Property(c => c.Contact2).HasMaxLength(255);
                entity.Property(c => c.PositionContact2).HasMaxLength(255);
                entity.Property(c => c.Website).HasMaxLength(255);
                entity.Property(c => c.PhoneNumber).HasMaxLength(50);
                entity.Property(c => c.Address).HasMaxLength(255);

                entity.HasOne(c => c.City).WithMany().HasForeignKey(c => c.CityCode).IsRequired().OnDelete(DeleteBehavior.Restrict);
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

            builder.Entity<AdmFondo>(entity => {
                entity.ToTable("AdmFondos");
                entity.HasKey(f => f.FundCode);
                entity.Property(f => f.FundCode).IsRequired().HasMaxLength(450).ValueGeneratedNever();

                entity.Property(f => f.VatcoFundCode);
                entity.Property(f => f.ClientCode);
                entity.Property(f => f.FundName).HasMaxLength(255);
                entity.Property(f => f.BranchCode);
                entity.Property(f => f.CityCode);
                entity.Property(f => f.CreationDate).HasColumnType("DATE");
                entity.Property(f => f.WithdrawalDate).HasColumnType("DATE");
                entity.Property(f => f.Cas4uCode).HasMaxLength(255);
                entity.Property(f => f.FundCurrency).HasMaxLength(50);
                entity.Property(f => f.FundType);

                entity.HasOne(f => f.Client).WithMany().HasForeignKey(f => f.ClientCode).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(f => f.Branch).WithMany().HasForeignKey(f => f.BranchCode).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(f => f.City).WithMany().HasForeignKey(f => f.CityCode).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AdmPunto>(entity =>
            {
                entity.ToTable("AdmPuntos");
                entity.HasKey(p => p.PointCode);
                entity.Property(p => p.PointCode).IsRequired().HasMaxLength(450);

                entity.Property(p => p.VatcoPointCode).HasMaxLength(255);
                entity.Property(p => p.ClientCode).IsRequired(false);
                entity.Property(p => p.ClientPointCode).HasMaxLength(255);
                entity.Property(p => p.MainClientCode).IsRequired(false);
                entity.Property(p => p.PointName).HasMaxLength(255);
                entity.Property(p => p.ShortName).HasMaxLength(255);
                entity.Property(p => p.BillingPoint).HasMaxLength(255);
                entity.Property(p => p.Address).HasMaxLength(255);
                entity.Property(p => p.PhoneNumber).HasMaxLength(50);
                entity.Property(p => p.Responsible).HasMaxLength(255);
                entity.Property(p => p.ResponsiblePosition).HasMaxLength(255);
                entity.Property(p => p.ResponsibleEmail).HasMaxLength(255);

                entity.HasOne(p => p.Client).WithMany().HasForeignKey(p => p.ClientCode).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Branch).WithMany().HasForeignKey(p => p.BranchCode).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.City).WithMany().HasForeignKey(p => p.CityCode).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Fund).WithMany().HasForeignKey(p => p.FundCode).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Route).WithMany().HasForeignKey(p => p.RouteBranchCode).IsRequired().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Range).WithMany().HasForeignKey(p => p.RangeCode).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AdmRange>(entity => {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).ValueGeneratedOnAdd();

                entity.Property(r => r.CodRange).IsRequired();
                entity.Property(r => r.RangeInformation);

                entity.Property(r => r.Lr1Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Lr1Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Lr2Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Lr2Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Lr3Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Lr3Hf).HasColumnType("TIME(0)");

                entity.Property(r => r.Mr1Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Mr1Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Mr2Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Mr2Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Mr3Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Mr3Hf).HasColumnType("TIME(0)");

                entity.Property(r => r.Wr1Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Wr1Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Wr2Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Wr2Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Wr3Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Wr3Hf).HasColumnType("TIME(0)");

                entity.Property(r => r.Jr1Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Jr1Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Jr2Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Jr2Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Jr3Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Jr3Hf).HasColumnType("TIME(0)");

                entity.Property(r => r.Vr1Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Vr1Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Vr2Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Vr2Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Vr3Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Vr3Hf).HasColumnType("TIME(0)");

                entity.Property(r => r.Sr1Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Sr1Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Sr2Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Sr2Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Sr3Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Sr3Hf).HasColumnType("TIME(0)");

                entity.Property(r => r.Dr1Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Dr1Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Dr2Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Dr2Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Dr3Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Dr3Hf).HasColumnType("TIME(0)");

                entity.Property(r => r.Fr1Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Fr1Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Fr2Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Fr2Hf).HasColumnType("TIME(0)");
                entity.Property(r => r.Fr3Hi).HasColumnType("TIME(0)");
                entity.Property(r => r.Fr3Hf).HasColumnType("TIME(0)");

                entity.HasOne(r => r.Client).WithMany().HasForeignKey(r => r.ClientId).OnDelete(DeleteBehavior.Restrict);

                string HHMM(string col) => $"LEFT(CONVERT(CHAR(8), [{col}], 108), 5)";
                string F(string col) => $"ISNULL({HHMM(col)}, '00:00')";
                string D(string flag, string a1, string b1, string a2, string b2, string a3, string b3)
                    => $"IIF([{flag}]=1,'1','0')+':'+{F(a1)}+'-'+{F(b1)}+','+{F(a2)}+'-'+{F(b2)}+','+{F(a3)}+'-'+{F(b3)}";

                var scheduleBody =
                    $"{D("Lunes", "Lr1Hi", "Lr1Hf", "Lr2Hi", "Lr2Hf", "Lr3Hi", "Lr3Hf")}+'|'+"
                  + $"{D("Martes", "Mr1Hi", "Mr1Hf", "Mr2Hi", "Mr2Hf", "Mr3Hi", "Mr3Hf")}+'|'+"
                  + $"{D("Miercoles", "Wr1Hi", "Wr1Hf", "Wr2Hi", "Wr2Hf", "Wr3Hi", "Wr3Hf")}+'|'+"
                  + $"{D("Jueves", "Jr1Hi", "Jr1Hf", "Jr2Hi", "Jr2Hf", "Jr3Hi", "Jr3Hf")}+'|'+"
                  + $"{D("Viernes", "Vr1Hi", "Vr1Hf", "Vr2Hi", "Vr2Hf", "Vr3Hi", "Vr3Hf")}+'|'+"
                  + $"{D("Sabado", "Sr1Hi", "Sr1Hf", "Sr2Hi", "Sr2Hf", "Sr3Hi", "Sr3Hf")}+'|'+"
                  + $"{D("Domingo", "Dr1Hi", "Dr1Hf", "Dr2Hi", "Dr2Hf", "Dr3Hi", "Dr3Hf")}+'|'+"
                  + $"{D("Festivo", "Fr1Hi", "Fr1Hf", "Fr2Hi", "Fr2Hf", "Fr3Hi", "Fr3Hf")}";

                var scheduleSql = $"CONVERT(VARCHAR(20), [CodCliente]) + ':' + ({scheduleBody})";

                entity.Property<string>("schedule_key")
                      .HasColumnName("schedule_key")
                      .HasComputedColumnSql(scheduleSql, stored: true);

                entity.HasIndex("schedule_key")
                      .HasFilter("[RangeStatus] = 1")
                      .IsUnique()
                      .HasDatabaseName("UX_AdmRangos_ScheduleKey");
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

            builder.Entity<AdmBankEntiy>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Name);
            });

            builder.Entity<CgsService>(entity =>
            {
                entity.ToTable("CgsServicios");
                entity.HasKey(s => s.ServiceOrderId);
                entity.Property(s => s.ServiceOrderId).IsRequired().ValueGeneratedNever();

                entity.Property(s => s.RequestNumber).HasMaxLength(255);
                entity.Property(s => s.ClientCode);
                entity.Property(s => s.ClientServiceOrderCode).HasMaxLength(255);
                entity.Property(s => s.BranchCode);
                entity.Property(s => s.RequestDate).HasColumnType("DATE").IsRequired();
                entity.Property(s => s.RequestTime).HasColumnType("TIME(0)").IsRequired();
                entity.Property(s => s.ConceptCode);
                entity.Property(s => s.TransferType).HasMaxLength(1);
                entity.Property(s => s.StatusCode);
                entity.Property(s => s.FlowCode);
                entity.Property(s => s.OriginClientCode);
                entity.Property(s => s.OriginPointCode).HasMaxLength(255).IsRequired();
                entity.Property(s => s.OriginIndicatorType).HasMaxLength(1).IsRequired();
                entity.Property(s => s.DestinationClientCode);
                entity.Property(s => s.DestinationPointCode).HasMaxLength(255).IsRequired();
                entity.Property(s => s.DestinationIndicatorType).HasMaxLength(1).IsRequired();
                entity.Property(s => s.AcceptanceDate).HasColumnType("DATE");
                entity.Property(s => s.AcceptanceTime).HasColumnType("TIME(0)");
                entity.Property(s => s.ProgrammingDate).HasColumnType("DATE");
                entity.Property(s => s.ProgrammingTime).HasColumnType("TIME(0)");
                entity.Property(s => s.InitialAttentionDate).HasColumnType("DATE");
                entity.Property(s => s.InitialAttentionTime).HasColumnType("TIME(0)");
                entity.Property(s => s.FinalAttentionDate).HasColumnType("DATE");
                entity.Property(s => s.FinalAttentionTime).HasColumnType("TIME(0)");
                entity.Property(s => s.CancellationDate).HasColumnType("DATE");
                entity.Property(s => s.CancellationTime).HasColumnType("TIME(0)");
                entity.Property(s => s.RejectionDate).HasColumnType("DATE");
                entity.Property(s => s.RejectionTime).HasColumnType("TIME(0)");
                entity.Property(s => s.IsFailed).IsRequired();
                entity.Property(s => s.FailedResponsible).HasMaxLength(255);
                entity.Property(s => s.CancellationPerson).HasMaxLength(255);
                entity.Property(s => s.CancellationOperator).HasMaxLength(255);
                entity.Property(s => s.ServiceModality).HasMaxLength(1);
                entity.Property(s => s.Observations).HasMaxLength(255);
                entity.Property(s => s.KeyValue).HasColumnType("INT");
                entity.Property(s => s.CgsOperatorId).HasMaxLength(450);
                entity.Property(s => s.CgsBranchName).HasMaxLength(255);
                entity.Property(s => s.OperatorIpAddress).HasMaxLength(50);
                entity.Property(s => s.BillValue).HasColumnType("DECIMAL(18,0)");
                entity.Property(s => s.CoinValue).HasColumnType("DECIMAL(18,0)");
                entity.Property(s => s.ServiceValue).HasColumnType("DECIMAL(18,0)");
                entity.Property(s => s.NumberOfChangeKits);
                entity.Property(s => s.NumberOfCoinBags);
                entity.Property(s => s.CancellationReason).HasMaxLength(450);
                entity.Property(s => s.DetailFile).HasMaxLength(450);

                entity.HasOne(s => s.Client).WithMany().HasForeignKey(s => s.ClientCode).IsRequired(false);
                entity.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchCode).IsRequired(false);
                entity.HasOne(s => s.Concept).WithMany().HasForeignKey(s => s.ConceptCode).IsRequired(false);
                entity.HasOne(s => s.Status).WithMany().HasForeignKey(s => s.StatusCode).IsRequired(false);
                entity.HasOne(s => s.OriginClient).WithMany().HasForeignKey(s => s.OriginClientCode).IsRequired(false);
                entity.HasOne(s => s.DestinationClient).WithMany().HasForeignKey(s => s.DestinationClientCode).IsRequired(false);
                entity.HasOne(s => s.CgsOperator).WithMany().HasForeignKey(s => s.CgsOperatorId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            });


            builder.Entity<CgsLocationType>(entity => {
                entity.ToTable("CgsTiposUbicacion");
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Id).IsRequired();
                entity.Property(l => l.TypeName).IsRequired().HasMaxLength(50);
            });

            builder.Entity<CefTransaction>(entity => {
                entity.ToTable("CefTransacciones");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Id).ValueGeneratedOnAdd();

                entity.Property(t => t.BranchCode).IsRequired();
                entity.Property(t => t.ServiceOrderId).IsRequired().HasColumnType("NVARCHAR(450)");
                entity.Property(t => t.RouteId).HasColumnType("VARCHAR(12)");
                entity.Property(t => t.SlipNumber).IsRequired();
                entity.Property(t => t.Currency).HasMaxLength(3);
                entity.Property(t => t.TransactionType).IsRequired().HasMaxLength(50);
                entity.Property(t => t.DeclaredBagCount).IsRequired();
                entity.Property(t => t.DeclaredEnvelopeCount).IsRequired();
                entity.Property(t => t.DeclaredCheckCount).IsRequired();
                entity.Property(t => t.DeclaredDocumentCount).IsRequired();
                entity.Property(t => t.DeclaredBillValue).IsRequired().HasColumnType("DECIMAL(18,0)");
                entity.Property(t => t.DeclaredCoinValue).IsRequired().HasColumnType("DECIMAL(18,0)");
                entity.Property(t => t.DeclaredDocumentValue).IsRequired().HasColumnType("DECIMAL(18,0)");
                entity.Property(t => t.TotalDeclaredValue).IsRequired().HasColumnType("DECIMAL(18,0)");
                entity.Property(t => t.TotalDeclaredValueInWords).HasMaxLength(255);
                entity.Property(t => t.TotalCountedValue).HasColumnType("DECIMAL(18,0)");
                // .HasComputedColumnSql("ISNULL((SELECT SUM(CVD.CalculatedAmount) FROM Cef_ValueDetails CVD JOIN Cef_Containers CC ON CVD.CefContainerId = CC.Id WHERE CC.CefTransactionId = Cef_Transactions.Id), 0)");
                entity.Property(t => t.TotalCountedValueInWords).HasMaxLength(255);
                entity.Property(t => t.ValueDifference).HasColumnType("DECIMAL(18,0)");
                // .HasComputedColumnSql("Cef_Transactions.TotalCountedValue - Cef_Transactions.TotalDeclaredValue");
                entity.Property(t => t.InformativeIncident).HasMaxLength(255);
                entity.Property(t => t.IsCustody).IsRequired();
                entity.Property(t => t.IsPointToPoint).IsRequired();
                entity.Property(t => t.TransactionStatus).IsRequired().HasMaxLength(50);
                entity.Property(t => t.RegistrationDate).IsRequired().HasColumnType("DATETIME");
                entity.Property(t => t.RegistrationUser).IsRequired().HasMaxLength(450);
                entity.Property(t => t.CountingStartDate).HasColumnType("DATETIME");
                entity.Property(t => t.CountingEndDate).HasColumnType("DATETIME");
                entity.Property(t => t.LastUpdateDate).HasColumnType("DATETIME");
                entity.Property(t => t.LastUpdateUser).HasMaxLength(450);
                entity.Property(t => t.RegistrationIP).HasMaxLength(50);

                // Relaciones
                entity.HasOne(t => t.Service).WithMany(s => s.CefTransactions).HasForeignKey(t => t.ServiceOrderId).HasPrincipalKey(s => s.ServiceOrderId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<TdvRutaDiaria>().WithMany().HasForeignKey(t => t.RouteId).HasPrincipalKey(r => r.Id).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.Branch).WithMany().HasForeignKey(t => t.BranchCode).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.RegistrationUser).HasPrincipalKey(u => u.Id).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.CountingUserBillId).HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.CountingUserCoinId).HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.ReviewerUserId).HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.VaultUserId).HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.LastUpdateUser).HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.ReceiverId).HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.DelivererId).HasPrincipalKey(u => u.Id).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CefContainer>(entity =>
            {
                entity.ToTable("CefBolsas");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).ValueGeneratedOnAdd();

                entity.Property(c => c.CefTransactionId).IsRequired();
                entity.Property(c => c.ContainerType).IsRequired().HasMaxLength(50);
                entity.Property(c => c.EnvelopeSubType).HasMaxLength(20).IsUnicode(false).IsRequired(false);
                entity.Property(c => c.ContainerCode).IsRequired().HasMaxLength(100);
                entity.Property(c => c.CountedValue).HasColumnType("DECIMAL(18,0)");
                // .HasComputedColumnSql("ISNULL((SELECT SUM(CVD.CalculatedAmount) FROM Cef_ValueDetails CVD WHERE CVD.CefContainerId = Cef_Containers.Id), 0)");
                entity.Property(c => c.ContainerStatus).IsRequired().HasMaxLength(50);
                entity.Property(c => c.Observations).HasMaxLength(255);
                entity.Property(c => c.ProcessingUserId).HasMaxLength(450);
                entity.Property(c => c.ProcessingDate).HasColumnType("DATETIME");
                entity.Property(c => c.ClientCashierId).HasColumnType("INT");
                entity.Property(c => c.ClientCashierName).HasMaxLength(255);
                entity.Property(c => c.ClientEnvelopeDate).HasColumnType("DATE");

                // Relaciones
                entity.HasOne(c => c.CefTransaction).WithMany(t => t.Containers).HasForeignKey(c => c.CefTransactionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(c => c.ParentContainer).WithMany(p => p.ChildContainers).HasForeignKey(c => c.ParentContainerId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.ProcessingUserId).HasPrincipalKey(u => u.Id).OnDelete(DeleteBehavior.Restrict);

                entity.Property(c => c.ContainerType).HasConversion<string>();
                entity.Property(c => c.ContainerStatus).HasConversion<string>();

                entity.HasIndex(c => c.ParentContainerId);
                entity.HasIndex(c => new { c.CefTransactionId, c.ContainerCode });

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_CEF_SOBRE_TipoSobreValido",
                        "([TipoBolsa] <> 'Sobre') OR ([TipoSobre] IN ('Efectivo','Documento','Cheque'))");

                    t.HasCheckConstraint(
                        "CK_CEF_SOBRE_Padre",
                        "(([TipoBolsa] = 'Sobre' AND [IdBolsaPadre] IS NOT NULL) OR " +
                        " ([TipoBolsa] <> 'Sobre' AND [IdBolsaPadre] IS NULL))");
                });
            });

            builder.Entity<CefValueDetail>(entity =>
            {
                entity.ToTable("CefDetallesValores");
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Id).ValueGeneratedOnAdd();

                entity.Property(v => v.CefContainerId).IsRequired();
                entity.Property(v => v.ValueType).IsRequired().HasMaxLength(50);
                entity.Property(v => v.Quantity).IsRequired();
                entity.Property(v => v.BundlesCount).IsRequired(false);
                entity.Property(v => v.LoosePiecesCount).IsRequired(false);
                entity.Property(v => v.UnitValue).HasColumnType("DECIMAL(18,0)");
                entity.Property(v => v.CalculatedAmount).HasColumnType("DECIMAL(18,0)");
                // .HasComputedColumnSql("ISNULL(d.Denomination, d.UnitValue) * d.Quantity", stored: true); // Almacenado para consistencia
                entity.Property(v => v.IssueDate).HasColumnType("DATE");
                entity.Property(v => v.Observations).HasMaxLength(255);

                // Relaciones
                entity.HasOne(c => c.CefContainer).WithMany(t => t.ValueDetails).HasForeignKey(c => c.CefContainerId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(c => c.AdmDenominacion).WithMany().HasForeignKey(c => c.DenominationId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.AdmQuality).WithMany().HasForeignKey(c => c.QualityId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.AdmBankEntitie).WithMany().HasForeignKey(c => c.EntitieBankId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.Property(v => v.ValueType).HasConversion<string>();
                entity.HasIndex(v => new { v.CefContainerId, v.ValueType, v.DenominationId, v.QualityId }).IsUnique(false);
            });

            builder.Entity<CefIncident>(entity =>
            {
                entity.ToTable("CefNovedades");
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Id).ValueGeneratedOnAdd();

                entity.Property(i => i.IncidentTypeId).IsRequired();
                entity.Property(i => i.AffectedAmount).IsRequired().HasColumnType("DECIMAL(18,0)");
                entity.Property(i => i.AffectedDenomination).IsRequired(false);
                entity.Property(i => i.AffectedQuantity).IsRequired(false);
                entity.Property(i => i.Description).IsRequired().HasMaxLength(255);
                entity.Property(i => i.ReportedUserId).IsRequired().HasMaxLength(450);
                entity.Property(i => i.IncidentDate).IsRequired().HasColumnType("DATETIME");
                entity.Property(i => i.IncidentStatus).IsRequired().HasMaxLength(50);

                // Relaciones
                entity.HasOne(i => i.CefTransaction).WithMany(t => t.Incidents).HasForeignKey(i => i.CefTransactionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.CefContainer).WithMany(c => c.Incidents).HasForeignKey(i => i.CefContainerId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.CefValueDetail).WithMany(vd => vd.Incidents).HasForeignKey(i => i.CefValueDetailId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.Denominacion).WithMany().HasForeignKey(i => i.AffectedDenomination).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.IncidentType).WithMany().HasForeignKey(i => i.IncidentTypeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(i => i.ReportedUserId).HasPrincipalKey(u => u.Id).OnDelete(DeleteBehavior.Restrict);
                entity.Property(i => i.IncidentStatus).HasConversion<string>();
            });


            builder.Entity<CefIncidentType>(entity =>
            {
                entity.ToTable("CefTiposNovedad");
                entity.HasKey(it => it.Id);
                entity.Property(it => it.Id).ValueGeneratedOnAdd();

                entity.Property(it => it.Code).IsRequired().HasMaxLength(50);
                entity.Property(it => it.Description).HasMaxLength(255);
                entity.Property(it => it.AppliesTo).IsRequired().HasMaxLength(50); // 'Container', 'ValueDetail', 'Transaction'

                entity.Property(it => it.AppliesTo).HasConversion<string>();
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

            builder.Entity<AdmQuality>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Id).ValueGeneratedOnAdd();

                entity.Property(q => q.QualityName).IsRequired().HasMaxLength(255);
                entity.Property(q => q.TypeOfMoney).IsRequired().HasMaxLength(1);
                entity.Property(q => q.DenominationFamily).IsRequired().HasMaxLength(1);
                entity.Property(q => q.Status).IsRequired();
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

            if (_branchContext != null)
            {
                builder.Entity<CgsService>()
                    .HasQueryFilter(s =>
                        _branchContext.AllBranches
                            ? (
                                !_branchContext.PermittedBranchIds.Any()
                                || _branchContext.PermittedBranchIds.Contains(s.BranchCode)
                              )
                            : (
                                !_branchContext.CurrentBranchId.HasValue
                                || s.BranchCode == _branchContext.CurrentBranchId
                              ));

                builder.Entity<CefTransaction>()
                    .HasQueryFilter(t =>
                        _branchContext.AllBranches
                            ? (
                                !_branchContext.PermittedBranchIds.Any()
                                || _branchContext.PermittedBranchIds.Contains(t.BranchCode)
                              )
                            : (
                                !_branchContext.CurrentBranchId.HasValue
                                || t.BranchCode == _branchContext.CurrentBranchId
                              ));
            }
        }
    }
}