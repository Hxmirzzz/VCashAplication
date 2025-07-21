using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialFullCleanSchemaFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmConceptos",
                columns: table => new
                {
                    CodConcepto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreConcepto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoConcepto = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmConceptos", x => x.CodConcepto);
                });

            migrationBuilder.CreateTable(
                name: "AdmConsecutivos",
                columns: table => new
                {
                    TipoConsecutivo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NombreConsecutivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LetraConsecutivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Inicio = table.Column<long>(type: "bigint", nullable: true),
                    Fin = table.Column<long>(type: "bigint", nullable: true),
                    ConsecutivoActual = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmConsecutivos", x => x.TipoConsecutivo);
                });

            migrationBuilder.CreateTable(
                name: "AdmDenominaciones",
                columns: table => new
                {
                    CodDenominacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoDenominacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Denominacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValorDenominacion = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    TipoDinero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamiliaDenominacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DivisaDenominacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnidadAgrupamiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CantidadUnidadAgrupamiento = table.Column<int>(type: "int", nullable: true),
                    TeclaAsociada = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnidadExistencias = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmDenominaciones", x => x.CodDenominacion);
                });

            migrationBuilder.CreateTable(
                name: "AdmEstados",
                columns: table => new
                {
                    CodEstado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreEstado = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmEstados", x => x.CodEstado);
                });

            migrationBuilder.CreateTable(
                name: "AdmPaises",
                columns: table => new
                {
                    CodPais = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NombrePais = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Siglas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmPaises", x => x.CodPais);
                });

            migrationBuilder.CreateTable(
                name: "AdmSucursales",
                columns: table => new
                {
                    CodSucursal = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreSucursal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LatitudSucursal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongitudSucursal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiglasSucursal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoSucursal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodBancoRepublica = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmSucursales", x => x.CodSucursal);
                });

            migrationBuilder.CreateTable(
                name: "AdmUnidades",
                columns: table => new
                {
                    CodUnidad = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NombreUnidad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoUnidad = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmUnidades", x => x.CodUnidad);
                });

            migrationBuilder.CreateTable(
                name: "AdmVistas",
                columns: table => new
                {
                    CodVista = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NombreVista = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RolAsociado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmVistas", x => x.CodVista);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NombreUsuario = table.Column<string>(type: "VARCHAR(50)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdmDepartamentos",
                columns: table => new
                {
                    CodDepartamento = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NombreDepartamento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodPais = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmDepartamentos", x => x.CodDepartamento);
                    table.ForeignKey(
                        name: "FK_AdmDepartamentos_AdmPaises_CodPais",
                        column: x => x.CodPais,
                        principalTable: "AdmPaises",
                        principalColumn: "CodPais");
                });

            migrationBuilder.CreateTable(
                name: "AdmRutas",
                columns: table => new
                {
                    CodRutaSuc = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CodRuta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreRuta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodSucursal = table.Column<int>(type: "int", nullable: true),
                    TipoRuta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoAtencion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Monto = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    Lunes = table.Column<bool>(type: "bit", nullable: false),
                    LunesHoraInicio = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    LunesHoraFin = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Martes = table.Column<bool>(type: "bit", nullable: false),
                    MartesHoraInicio = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    MartesHoraFin = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Miercoles = table.Column<bool>(type: "bit", nullable: false),
                    MiercolesHoraInicio = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    MiercolesHoraFin = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Jueves = table.Column<bool>(type: "bit", nullable: false),
                    JuevesHoraInicio = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    JuevesHoraFin = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Viernes = table.Column<bool>(type: "bit", nullable: false),
                    ViernesHoraInicio = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    ViernesHoraFin = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Sabado = table.Column<bool>(type: "bit", nullable: false),
                    SabadoHoraInicio = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    SabadoHoraFin = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Domingo = table.Column<bool>(type: "bit", nullable: false),
                    DomingoHoraInicio = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    DomingoHoraFin = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Festivo = table.Column<bool>(type: "bit", nullable: false),
                    FestivoHoraInicio = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    FestivoHoraFin = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    ConcatenadoRango = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstadoRuta = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmRutas", x => x.CodRutaSuc);
                    table.ForeignKey(
                        name: "FK_AdmRutas_AdmSucursales_CodSucursal",
                        column: x => x.CodSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal");
                });

            migrationBuilder.CreateTable(
                name: "AdmServicios",
                columns: table => new
                {
                    OrdenServicio = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NumeroPedido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCliente = table.Column<int>(type: "int", nullable: true),
                    CodigoOsCliente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodSucursal = table.Column<int>(type: "int", nullable: true),
                    NombreSucursal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaSolicitud = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraSolicitud = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    CodConcepto = table.Column<int>(type: "int", nullable: true),
                    TipoConcepto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoTraslado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodEstado = table.Column<int>(type: "int", nullable: true),
                    CodFlujo = table.Column<int>(type: "int", nullable: true),
                    CodClienteOrigen = table.Column<int>(type: "int", nullable: true),
                    ClienteOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PuntoOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCiudadOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodSucursalOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodRangoOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndicadorOrigenTipo = table.Column<bool>(type: "bit", nullable: false),
                    CodClienteDestino = table.Column<int>(type: "int", nullable: true),
                    ClienteDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PuntoDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCiudadDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodSucursalDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodRangoDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndicadorDestinoTipo = table.Column<bool>(type: "bit", nullable: false),
                    FechaAceptacion = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraAceptacion = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    FechaProgramacion = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraProgramacion = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    FechaAtencionInicial = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraAtencionInicial = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    FechaAtencionFinal = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraAtencionFinal = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    FechaCancelacion = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraCancelacion = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    FechaRechazo = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraRechazo = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    Fallido = table.Column<bool>(type: "bit", nullable: false),
                    ResponsableFallido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonaCancelacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperadorCancelacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModalidadServicio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Clave = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperadorCgs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SucursalCgs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpOperador = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValorServicio = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    NumeroKitsCambio = table.Column<int>(type: "int", nullable: true),
                    NumeroBolsasMoneda = table.Column<int>(type: "int", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchivoDetalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValorBillete = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    ValorMoneda = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmServicios", x => x.OrdenServicio);
                    table.ForeignKey(
                        name: "FK_AdmServicios_AdmConceptos_CodConcepto",
                        column: x => x.CodConcepto,
                        principalTable: "AdmConceptos",
                        principalColumn: "CodConcepto");
                    table.ForeignKey(
                        name: "FK_AdmServicios_AdmEstados_CodEstado",
                        column: x => x.CodEstado,
                        principalTable: "AdmEstados",
                        principalColumn: "CodEstado");
                    table.ForeignKey(
                        name: "FK_AdmServicios_AdmSucursales_CodSucursal",
                        column: x => x.CodSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal");
                });

            migrationBuilder.CreateTable(
                name: "AdmCargos",
                columns: table => new
                {
                    CodCargo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreCargo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodUnidad = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Jornada = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    TiempoBreak = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Adicional = table.Column<bool>(type: "bit", nullable: false),
                    Recargos = table.Column<bool>(type: "bit", nullable: false),
                    Extras = table.Column<bool>(type: "bit", nullable: false),
                    CentroCosto = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmCargos", x => x.CodCargo);
                    table.ForeignKey(
                        name: "FK_AdmCargos_AdmUnidades_CodUnidad",
                        column: x => x.CodUnidad,
                        principalTable: "AdmUnidades",
                        principalColumn: "CodUnidad");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermisosPerfil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodPerfilId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PerfilId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CodVista = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PuedeVer = table.Column<bool>(type: "bit", nullable: false),
                    PuedeCrear = table.Column<bool>(type: "bit", nullable: false),
                    PuedeEditar = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisosPerfil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermisosPerfil_AdmVistas_CodVista",
                        column: x => x.CodVista,
                        principalTable: "AdmVistas",
                        principalColumn: "CodVista",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermisosPerfil_AspNetRoles_CodPerfilId",
                        column: x => x.CodPerfilId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermisosPerfil_AspNetRoles_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdmCiudades",
                columns: table => new
                {
                    CodCiudad = table.Column<int>(type: "int", nullable: false),
                    NombreCiudad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodDepartamento = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CodPais = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CodSucursal = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmCiudades", x => x.CodCiudad);
                    table.ForeignKey(
                        name: "FK_AdmCiudades_AdmDepartamentos_CodDepartamento",
                        column: x => x.CodDepartamento,
                        principalTable: "AdmDepartamentos",
                        principalColumn: "CodDepartamento");
                    table.ForeignKey(
                        name: "FK_AdmCiudades_AdmPaises_CodPais",
                        column: x => x.CodPais,
                        principalTable: "AdmPaises",
                        principalColumn: "CodPais");
                    table.ForeignKey(
                        name: "FK_AdmCiudades_AdmSucursales_CodSucursal",
                        column: x => x.CodSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal");
                });

            migrationBuilder.CreateTable(
                name: "AdmEmpleados",
                columns: table => new
                {
                    CodCedula = table.Column<int>(type: "int", nullable: false),
                    PrimerApellido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SegundoApellido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimerNombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SegundoNombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoDocumento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroCarnet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaExpedicion = table.Column<DateOnly>(type: "date", nullable: true),
                    CiudadExpedicion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCargo = table.Column<int>(type: "int", nullable: true),
                    CodSucursal = table.Column<int>(type: "int", nullable: true),
                    Celular = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Genero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtroGenero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FecVinculacion = table.Column<DateOnly>(type: "date", nullable: true),
                    FecRetiro = table.Column<DateOnly>(type: "date", nullable: true),
                    IndicadorCatalogo = table.Column<bool>(type: "bit", nullable: false),
                    IngresoRepublica = table.Column<bool>(type: "bit", nullable: false),
                    IngresoAeropuerto = table.Column<bool>(type: "bit", nullable: false),
                    FotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirmaUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmpleadoEstado = table.Column<int>(type: "int", nullable: true),
                    UsuarioRegistroId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmEmpleados", x => x.CodCedula);
                    table.ForeignKey(
                        name: "FK_AdmEmpleados_AdmCargos_CodCargo",
                        column: x => x.CodCargo,
                        principalTable: "AdmCargos",
                        principalColumn: "CodCargo");
                    table.ForeignKey(
                        name: "FK_AdmEmpleados_AdmSucursales_CodSucursal",
                        column: x => x.CodSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal");
                    table.ForeignKey(
                        name: "FK_AdmEmpleados_AspNetUsers_UsuarioRegistroId",
                        column: x => x.UsuarioRegistroId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AdmVehiculos",
                columns: table => new
                {
                    CodVehiculo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CodigoVatco = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodSucursal = table.Column<int>(type: "int", nullable: true),
                    TipoVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Toneladas = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: true),
                    CodUnidad = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Propiedad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmpresaAlquiler = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MontoAutorizado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    ConductorCedula = table.Column<int>(type: "int", nullable: true),
                    GPS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroSoat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VencimientoSoat = table.Column<DateOnly>(type: "DATE", nullable: true),
                    NumeroTecnomecanica = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VencimientoTecnomecanica = table.Column<DateOnly>(type: "DATE", nullable: true),
                    Marca = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Linea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Blindaje = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroSerie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroChasis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmVehiculos", x => x.CodVehiculo);
                    table.ForeignKey(
                        name: "FK_AdmVehiculos_AdmEmpleados_ConductorCedula",
                        column: x => x.ConductorCedula,
                        principalTable: "AdmEmpleados",
                        principalColumn: "CodCedula");
                    table.ForeignKey(
                        name: "FK_AdmVehiculos_AdmSucursales_CodSucursal",
                        column: x => x.CodSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal");
                    table.ForeignKey(
                        name: "FK_AdmVehiculos_AdmUnidades_CodUnidad",
                        column: x => x.CodUnidad,
                        principalTable: "AdmUnidades",
                        principalColumn: "CodUnidad");
                });

            migrationBuilder.CreateTable(
                name: "SegRegistroEmpleados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodCedula = table.Column<int>(type: "int", nullable: false),
                    CodCargo = table.Column<int>(type: "int", nullable: false),
                    CodUnidad = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CodSucursal = table.Column<int>(type: "int", nullable: false),
                    FechaEntrada = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraEntrada = table.Column<TimeOnly>(type: "time", nullable: false),
                    FechaSalida = table.Column<DateOnly>(type: "date", nullable: true),
                    HoraSalida = table.Column<TimeOnly>(type: "time", nullable: true),
                    IndicadorEntrada = table.Column<bool>(type: "bit", nullable: false),
                    IndicadorSalida = table.Column<bool>(type: "bit", nullable: false),
                    RegistroUsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PrimerNombreEmpleado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SegundoNombreEmpleado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimerApellidoEmpleado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SegundoApellidoEmpleado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreCargoEmpleado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreUnidadEmpleado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreSucursalEmpleado = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SegRegistroEmpleados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SegRegistroEmpleados_AdmCargos_CodCargo",
                        column: x => x.CodCargo,
                        principalTable: "AdmCargos",
                        principalColumn: "CodCargo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SegRegistroEmpleados_AdmEmpleados_CodCedula",
                        column: x => x.CodCedula,
                        principalTable: "AdmEmpleados",
                        principalColumn: "CodCedula",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SegRegistroEmpleados_AdmSucursales_CodSucursal",
                        column: x => x.CodSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SegRegistroEmpleados_AdmUnidades_CodUnidad",
                        column: x => x.CodUnidad,
                        principalTable: "AdmUnidades",
                        principalColumn: "CodUnidad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SegRegistroEmpleados_AspNetUsers_RegistroUsuarioId",
                        column: x => x.RegistroUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TdvRutasDiarias",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(12)", nullable: false),
                    CodRutaSuc = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NombreRuta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoRuta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaEjecucion = table.Column<DateOnly>(type: "date", nullable: false),
                    CodSucursal = table.Column<int>(type: "int", nullable: false),
                    NombreSucursal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodVehiculo = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CedulaJT = table.Column<int>(type: "int", nullable: true),
                    NombreJT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCargoJT = table.Column<int>(type: "int", nullable: true),
                    FechaIngresoJT = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraIngresoJT = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    FechaSalidaJT = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraSalidaJT = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    CedulaConductor = table.Column<int>(type: "int", nullable: true),
                    NombreConductor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCargoConductor = table.Column<int>(type: "int", nullable: true),
                    CedulaTripulante = table.Column<int>(type: "int", nullable: true),
                    NombreTripulante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCargoTripulante = table.Column<int>(type: "int", nullable: true),
                    FechaPlaneacion = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraPlaneacion = table.Column<TimeOnly>(type: "time", nullable: false),
                    UsuarioPlaneacion = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCargue = table.Column<DateOnly>(type: "date", nullable: true),
                    HoraCargue = table.Column<TimeOnly>(type: "time", nullable: true),
                    CantBolsaBilleteEntrega = table.Column<int>(type: "int", nullable: true),
                    CantBolsaMonedaEntrega = table.Column<int>(type: "int", nullable: true),
                    UsuarioCEFCargue = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaDescargue = table.Column<DateOnly>(type: "date", nullable: true),
                    HoraDescargue = table.Column<TimeOnly>(type: "time", nullable: true),
                    CantBolsaBilleteRecibe = table.Column<int>(type: "int", nullable: true),
                    CantBolsaMonedaRecibe = table.Column<int>(type: "int", nullable: true),
                    UsuarioCEFDescargue = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    KmInicial = table.Column<decimal>(type: "NUMERIC(18,0)", nullable: true),
                    FechaSalidaRuta = table.Column<DateOnly>(type: "date", nullable: true),
                    HoraSalidaRuta = table.Column<TimeOnly>(type: "time", nullable: true),
                    UsuarioSupervisorApertura = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    KmFinal = table.Column<decimal>(type: "NUMERIC(18,0)", nullable: true),
                    FechaEntradaRuta = table.Column<DateOnly>(type: "date", nullable: true),
                    HoraEntradaRuta = table.Column<TimeOnly>(type: "time", nullable: true),
                    UsuarioSupervisorCierre = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TdvRutasDiarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AdmCargos_CodCargoConductor",
                        column: x => x.CodCargoConductor,
                        principalTable: "AdmCargos",
                        principalColumn: "CodCargo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AdmCargos_CodCargoJT",
                        column: x => x.CodCargoJT,
                        principalTable: "AdmCargos",
                        principalColumn: "CodCargo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AdmCargos_CodCargoTripulante",
                        column: x => x.CodCargoTripulante,
                        principalTable: "AdmCargos",
                        principalColumn: "CodCargo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AdmEmpleados_CedulaConductor",
                        column: x => x.CedulaConductor,
                        principalTable: "AdmEmpleados",
                        principalColumn: "CodCedula",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AdmEmpleados_CedulaJT",
                        column: x => x.CedulaJT,
                        principalTable: "AdmEmpleados",
                        principalColumn: "CodCedula",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AdmEmpleados_CedulaTripulante",
                        column: x => x.CedulaTripulante,
                        principalTable: "AdmEmpleados",
                        principalColumn: "CodCedula",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AdmRutas_CodRutaSuc",
                        column: x => x.CodRutaSuc,
                        principalTable: "AdmRutas",
                        principalColumn: "CodRutaSuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AdmSucursales_CodSucursal",
                        column: x => x.CodSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AdmVehiculos_CodVehiculo",
                        column: x => x.CodVehiculo,
                        principalTable: "AdmVehiculos",
                        principalColumn: "CodVehiculo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AspNetUsers_UsuarioCEFCargue",
                        column: x => x.UsuarioCEFCargue,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AspNetUsers_UsuarioCEFDescargue",
                        column: x => x.UsuarioCEFDescargue,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AspNetUsers_UsuarioPlaneacion",
                        column: x => x.UsuarioPlaneacion,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AspNetUsers_UsuarioSupervisorApertura",
                        column: x => x.UsuarioSupervisorApertura,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TdvRutasDiarias_AspNetUsers_UsuarioSupervisorCierre",
                        column: x => x.UsuarioSupervisorCierre,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmCargos_CodUnidad",
                table: "AdmCargos",
                column: "CodUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_AdmCiudades_CodDepartamento",
                table: "AdmCiudades",
                column: "CodDepartamento");

            migrationBuilder.CreateIndex(
                name: "IX_AdmCiudades_CodPais",
                table: "AdmCiudades",
                column: "CodPais");

            migrationBuilder.CreateIndex(
                name: "IX_AdmCiudades_CodSucursal",
                table: "AdmCiudades",
                column: "CodSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_AdmDepartamentos_CodPais",
                table: "AdmDepartamentos",
                column: "CodPais");

            migrationBuilder.CreateIndex(
                name: "IX_AdmEmpleados_CodCargo",
                table: "AdmEmpleados",
                column: "CodCargo");

            migrationBuilder.CreateIndex(
                name: "IX_AdmEmpleados_CodSucursal",
                table: "AdmEmpleados",
                column: "CodSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_AdmEmpleados_UsuarioRegistroId",
                table: "AdmEmpleados",
                column: "UsuarioRegistroId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmRutas_CodSucursal",
                table: "AdmRutas",
                column: "CodSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_AdmServicios_CodConcepto",
                table: "AdmServicios",
                column: "CodConcepto");

            migrationBuilder.CreateIndex(
                name: "IX_AdmServicios_CodEstado",
                table: "AdmServicios",
                column: "CodEstado");

            migrationBuilder.CreateIndex(
                name: "IX_AdmServicios_CodSucursal",
                table: "AdmServicios",
                column: "CodSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_AdmVehiculos_CodSucursal",
                table: "AdmVehiculos",
                column: "CodSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_AdmVehiculos_CodUnidad",
                table: "AdmVehiculos",
                column: "CodUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_AdmVehiculos_ConductorCedula",
                table: "AdmVehiculos",
                column: "ConductorCedula");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NombreUsuario",
                table: "AspNetUsers",
                column: "NombreUsuario",
                unique: true,
                filter: "[NombreUsuario] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PermisosPerfil_CodPerfilId",
                table: "PermisosPerfil",
                column: "CodPerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_PermisosPerfil_CodVista",
                table: "PermisosPerfil",
                column: "CodVista");

            migrationBuilder.CreateIndex(
                name: "IX_PermisosPerfil_PerfilId",
                table: "PermisosPerfil",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_CodCargo",
                table: "SegRegistroEmpleados",
                column: "CodCargo");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_CodCedula",
                table: "SegRegistroEmpleados",
                column: "CodCedula");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_CodSucursal",
                table: "SegRegistroEmpleados",
                column: "CodSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_CodUnidad",
                table: "SegRegistroEmpleados",
                column: "CodUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_RegistroUsuarioId",
                table: "SegRegistroEmpleados",
                column: "RegistroUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_CedulaConductor",
                table: "TdvRutasDiarias",
                column: "CedulaConductor");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_CedulaJT",
                table: "TdvRutasDiarias",
                column: "CedulaJT");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_CedulaTripulante",
                table: "TdvRutasDiarias",
                column: "CedulaTripulante");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_CodCargoConductor",
                table: "TdvRutasDiarias",
                column: "CodCargoConductor");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_CodCargoJT",
                table: "TdvRutasDiarias",
                column: "CodCargoJT");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_CodCargoTripulante",
                table: "TdvRutasDiarias",
                column: "CodCargoTripulante");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_CodRutaSuc",
                table: "TdvRutasDiarias",
                column: "CodRutaSuc");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_CodSucursal",
                table: "TdvRutasDiarias",
                column: "CodSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_CodVehiculo",
                table: "TdvRutasDiarias",
                column: "CodVehiculo");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_UsuarioCEFCargue",
                table: "TdvRutasDiarias",
                column: "UsuarioCEFCargue");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_UsuarioCEFDescargue",
                table: "TdvRutasDiarias",
                column: "UsuarioCEFDescargue");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_UsuarioPlaneacion",
                table: "TdvRutasDiarias",
                column: "UsuarioPlaneacion");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_UsuarioSupervisorApertura",
                table: "TdvRutasDiarias",
                column: "UsuarioSupervisorApertura");

            migrationBuilder.CreateIndex(
                name: "IX_TdvRutasDiarias_UsuarioSupervisorCierre",
                table: "TdvRutasDiarias",
                column: "UsuarioSupervisorCierre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmCiudades");

            migrationBuilder.DropTable(
                name: "AdmConsecutivos");

            migrationBuilder.DropTable(
                name: "AdmDenominaciones");

            migrationBuilder.DropTable(
                name: "AdmServicios");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "PermisosPerfil");

            migrationBuilder.DropTable(
                name: "SegRegistroEmpleados");

            migrationBuilder.DropTable(
                name: "TdvRutasDiarias");

            migrationBuilder.DropTable(
                name: "AdmDepartamentos");

            migrationBuilder.DropTable(
                name: "AdmConceptos");

            migrationBuilder.DropTable(
                name: "AdmEstados");

            migrationBuilder.DropTable(
                name: "AdmVistas");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AdmRutas");

            migrationBuilder.DropTable(
                name: "AdmVehiculos");

            migrationBuilder.DropTable(
                name: "AdmPaises");

            migrationBuilder.DropTable(
                name: "AdmEmpleados");

            migrationBuilder.DropTable(
                name: "AdmCargos");

            migrationBuilder.DropTable(
                name: "AdmSucursales");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "AdmUnidades");
        }
    }
}
