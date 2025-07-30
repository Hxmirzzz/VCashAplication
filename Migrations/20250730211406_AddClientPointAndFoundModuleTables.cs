using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPointAndFoundModuleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefTransacciones_AdmServicios_OrdenServicio",
                table: "CefTransacciones");

            migrationBuilder.DropTable(
                name: "AdmServicios");

            migrationBuilder.AlterColumn<decimal>(
                name: "ValorContado",
                table: "CefContenedores",
                type: "DECIMAL(18,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,0)");

            migrationBuilder.CreateTable(
                name: "AdmClientes",
                columns: table => new
                {
                    CodigoCliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientePrincipal = table.Column<int>(type: "int", nullable: true),
                    NombreCliente = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SiglasCliente = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<int>(type: "int", nullable: false),
                    CodCiudad = table.Column<int>(type: "int", nullable: false),
                    CiudadFacturacion = table.Column<int>(type: "int", nullable: true),
                    Contacto1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CargoContacto1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Contacto2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CargoContacto2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TipoCliente = table.Column<int>(type: "int", nullable: false),
                    PaginaWeb = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmClientes", x => x.CodigoCliente);
                    table.ForeignKey(
                        name: "FK_AdmClientes_AdmCiudades_CodCiudad",
                        column: x => x.CodCiudad,
                        principalTable: "AdmCiudades",
                        principalColumn: "CodCiudad",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmFondos",
                columns: table => new
                {
                    CodigoFondo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CodigoFondoVatco = table.Column<int>(type: "int", nullable: true),
                    CodigoCliente = table.Column<int>(type: "int", nullable: true),
                    NombreFondo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodigoSucursal = table.Column<int>(type: "int", nullable: true),
                    CodigoCiudad = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateOnly>(type: "DATE", nullable: true),
                    FechaRetiro = table.Column<DateOnly>(type: "DATE", nullable: true),
                    CodCas4u = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DivisaFondo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TipoFondo = table.Column<int>(type: "int", nullable: true),
                    EstadoFondo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmFondos", x => x.CodigoFondo);
                    table.ForeignKey(
                        name: "FK_AdmFondos_AdmCiudades_CodigoCiudad",
                        column: x => x.CodigoCiudad,
                        principalTable: "AdmCiudades",
                        principalColumn: "CodCiudad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmFondos_AdmClientes_CodigoCliente",
                        column: x => x.CodigoCliente,
                        principalTable: "AdmClientes",
                        principalColumn: "CodigoCliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmFondos_AdmSucursales_CodigoSucursal",
                        column: x => x.CodigoSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CgsServicios",
                columns: table => new
                {
                    OrdenServicio = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    NumeroPedido = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodCliente = table.Column<int>(type: "int", nullable: false),
                    CodOsCliente = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodSucursal = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateOnly>(type: "DATE", nullable: false),
                    HoraSolicitud = table.Column<TimeOnly>(type: "TIME(0)", nullable: false),
                    CodConcepto = table.Column<int>(type: "int", nullable: false),
                    TipoTraslado = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    CodEstado = table.Column<int>(type: "int", nullable: false),
                    CodFlujo = table.Column<int>(type: "int", nullable: true),
                    CodClienteOrigen = table.Column<int>(type: "int", nullable: true),
                    CodPuntoOrigen = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    IndicadorTipoOrigen = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    CodClienteDestino = table.Column<int>(type: "int", nullable: true),
                    CodPuntoDestino = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IndicadorTipoDestino = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
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
                    ResponsableFallido = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PersonaCancelacion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    OperadorCancelacion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModalidadServicio = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Clave = table.Column<int>(type: "INT", nullable: true),
                    OperadorCgsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SucursalCgs = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IpOperador = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ValorBillete = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    ValorMoneda = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    ValorServicio = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    NumeroKitsCambio = table.Column<int>(type: "int", nullable: true),
                    NumeroBolsasMoneda = table.Column<int>(type: "int", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ArchivoDetalle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CgsServicios", x => x.OrdenServicio);
                    table.ForeignKey(
                        name: "FK_CgsServicios_AdmClientes_CodCliente",
                        column: x => x.CodCliente,
                        principalTable: "AdmClientes",
                        principalColumn: "CodigoCliente");
                    table.ForeignKey(
                        name: "FK_CgsServicios_AdmClientes_CodClienteDestino",
                        column: x => x.CodClienteDestino,
                        principalTable: "AdmClientes",
                        principalColumn: "CodigoCliente");
                    table.ForeignKey(
                        name: "FK_CgsServicios_AdmClientes_CodClienteOrigen",
                        column: x => x.CodClienteOrigen,
                        principalTable: "AdmClientes",
                        principalColumn: "CodigoCliente");
                    table.ForeignKey(
                        name: "FK_CgsServicios_AdmConceptos_CodConcepto",
                        column: x => x.CodConcepto,
                        principalTable: "AdmConceptos",
                        principalColumn: "CodConcepto");
                    table.ForeignKey(
                        name: "FK_CgsServicios_AdmEstados_CodEstado",
                        column: x => x.CodEstado,
                        principalTable: "AdmEstados",
                        principalColumn: "CodEstado");
                    table.ForeignKey(
                        name: "FK_CgsServicios_AdmSucursales_CodSucursal",
                        column: x => x.CodSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal");
                    table.ForeignKey(
                        name: "FK_CgsServicios_AspNetUsers_OperadorCgsId",
                        column: x => x.OperadorCgsId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmPuntos",
                columns: table => new
                {
                    CodigoPunto = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CodPuntoVatco = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodigoCliente = table.Column<int>(type: "int", nullable: true),
                    CodPuntoCliente = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodClientePrincipal = table.Column<int>(type: "int", nullable: true),
                    NombrePunto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NombreCorto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PuntoFacturacion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Responsable = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CargoResponsable = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CorreoResponsable = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodigoSucursal = table.Column<int>(type: "int", nullable: true),
                    CodigoCiudad = table.Column<int>(type: "int", nullable: true),
                    Latitud = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Longitud = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RadioPunto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BaseCambio = table.Column<bool>(type: "bit", nullable: false),
                    LlavesPunto = table.Column<bool>(type: "bit", nullable: false),
                    SobresPunto = table.Column<bool>(type: "bit", nullable: false),
                    ChequesPunto = table.Column<bool>(type: "bit", nullable: false),
                    FondoPunto = table.Column<int>(type: "int", nullable: true),
                    CodigoFondo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TrasladoPunto = table.Column<bool>(type: "bit", nullable: false),
                    CoberturaPunto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FechaIngreso = table.Column<DateOnly>(type: "DATE", nullable: true),
                    FechaRetiro = table.Column<DateOnly>(type: "DATE", nullable: true),
                    TipoPunto = table.Column<int>(type: "int", nullable: true),
                    CodigoRutaSuc = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TipoNegocio = table.Column<int>(type: "int", nullable: true),
                    DocumentosPunto = table.Column<bool>(type: "bit", nullable: false),
                    ExistenciasPunto = table.Column<bool>(type: "bit", nullable: false),
                    PrediccionPunto = table.Column<bool>(type: "bit", nullable: false),
                    CustodiaPunto = table.Column<bool>(type: "bit", nullable: false),
                    OtrosValoresPunto = table.Column<bool>(type: "bit", nullable: false),
                    Otros = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LiberacionEfectivoPunto = table.Column<bool>(type: "bit", nullable: false),
                    EscalaInterurbanos = table.Column<int>(type: "int", nullable: true),
                    CodCas4u = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NivelRiesgo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodigoRango = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    InfoRangoAtencion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Bateria = table.Column<bool>(type: "bit", nullable: false),
                    BateriaAtm = table.Column<int>(type: "int", nullable: true),
                    LocalizacionAtm = table.Column<int>(type: "int", nullable: true),
                    EmergenciaAtm = table.Column<bool>(type: "bit", nullable: true),
                    PrimeraProvision = table.Column<DateOnly>(type: "DATE", nullable: true),
                    MarcaAtm = table.Column<int>(type: "int", nullable: true),
                    ModalidadAtm = table.Column<int>(type: "int", nullable: true),
                    CodigoSeteo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DivisaAtm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SolicitudWsAtm = table.Column<int>(type: "int", nullable: true),
                    TipoAtm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PorcentajeAgotamiento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CriticidadAtm = table.Column<int>(type: "int", nullable: true),
                    Consignacion = table.Column<int>(type: "int", nullable: true),
                    CodigoComposicion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmPuntos", x => x.CodigoPunto);
                    table.ForeignKey(
                        name: "FK_AdmPuntos_AdmCiudades_CodigoCiudad",
                        column: x => x.CodigoCiudad,
                        principalTable: "AdmCiudades",
                        principalColumn: "CodCiudad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmPuntos_AdmClientes_CodigoCliente",
                        column: x => x.CodigoCliente,
                        principalTable: "AdmClientes",
                        principalColumn: "CodigoCliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmPuntos_AdmFondos_CodigoFondo",
                        column: x => x.CodigoFondo,
                        principalTable: "AdmFondos",
                        principalColumn: "CodigoFondo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmPuntos_AdmRutas_CodigoRutaSuc",
                        column: x => x.CodigoRutaSuc,
                        principalTable: "AdmRutas",
                        principalColumn: "CodRutaSuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmPuntos_AdmSucursales_CodigoSucursal",
                        column: x => x.CodigoSucursal,
                        principalTable: "AdmSucursales",
                        principalColumn: "CodSucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmClientes_CodCiudad",
                table: "AdmClientes",
                column: "CodCiudad");

            migrationBuilder.CreateIndex(
                name: "IX_AdmFondos_CodigoCiudad",
                table: "AdmFondos",
                column: "CodigoCiudad");

            migrationBuilder.CreateIndex(
                name: "IX_AdmFondos_CodigoCliente",
                table: "AdmFondos",
                column: "CodigoCliente");

            migrationBuilder.CreateIndex(
                name: "IX_AdmFondos_CodigoSucursal",
                table: "AdmFondos",
                column: "CodigoSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_AdmPuntos_CodigoCiudad",
                table: "AdmPuntos",
                column: "CodigoCiudad");

            migrationBuilder.CreateIndex(
                name: "IX_AdmPuntos_CodigoCliente",
                table: "AdmPuntos",
                column: "CodigoCliente");

            migrationBuilder.CreateIndex(
                name: "IX_AdmPuntos_CodigoFondo",
                table: "AdmPuntos",
                column: "CodigoFondo");

            migrationBuilder.CreateIndex(
                name: "IX_AdmPuntos_CodigoRutaSuc",
                table: "AdmPuntos",
                column: "CodigoRutaSuc");

            migrationBuilder.CreateIndex(
                name: "IX_AdmPuntos_CodigoSucursal",
                table: "AdmPuntos",
                column: "CodigoSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_CgsServicios_CodCliente",
                table: "CgsServicios",
                column: "CodCliente");

            migrationBuilder.CreateIndex(
                name: "IX_CgsServicios_CodClienteDestino",
                table: "CgsServicios",
                column: "CodClienteDestino");

            migrationBuilder.CreateIndex(
                name: "IX_CgsServicios_CodClienteOrigen",
                table: "CgsServicios",
                column: "CodClienteOrigen");

            migrationBuilder.CreateIndex(
                name: "IX_CgsServicios_CodConcepto",
                table: "CgsServicios",
                column: "CodConcepto");

            migrationBuilder.CreateIndex(
                name: "IX_CgsServicios_CodEstado",
                table: "CgsServicios",
                column: "CodEstado");

            migrationBuilder.CreateIndex(
                name: "IX_CgsServicios_CodSucursal",
                table: "CgsServicios",
                column: "CodSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_CgsServicios_OperadorCgsId",
                table: "CgsServicios",
                column: "OperadorCgsId");

            migrationBuilder.AddForeignKey(
                name: "FK_CefTransacciones_CgsServicios_OrdenServicio",
                table: "CefTransacciones",
                column: "OrdenServicio",
                principalTable: "CgsServicios",
                principalColumn: "OrdenServicio",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefTransacciones_CgsServicios_OrdenServicio",
                table: "CefTransacciones");

            migrationBuilder.DropTable(
                name: "AdmPuntos");

            migrationBuilder.DropTable(
                name: "CgsServicios");

            migrationBuilder.DropTable(
                name: "AdmFondos");

            migrationBuilder.DropTable(
                name: "AdmClientes");

            migrationBuilder.AlterColumn<decimal>(
                name: "ValorContado",
                table: "CefContenedores",
                type: "DECIMAL(18,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,0)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AdmServicios",
                columns: table => new
                {
                    OrdenServicio = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CodConcepto = table.Column<int>(type: "int", nullable: true),
                    CodEstado = table.Column<int>(type: "int", nullable: true),
                    CodSucursal = table.Column<int>(type: "int", nullable: true),
                    ArchivoDetalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Clave = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClienteDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClienteOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCiudadDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCiudadOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodCliente = table.Column<int>(type: "int", nullable: true),
                    CodClienteDestino = table.Column<int>(type: "int", nullable: true),
                    CodClienteOrigen = table.Column<int>(type: "int", nullable: true),
                    CodDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodFlujo = table.Column<int>(type: "int", nullable: true),
                    CodOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodRangoDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodRangoOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodSucursalDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodSucursalOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodigoOsCliente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fallido = table.Column<bool>(type: "bit", nullable: false),
                    FechaAceptacion = table.Column<DateOnly>(type: "DATE", nullable: true),
                    FechaAtencionFinal = table.Column<DateOnly>(type: "DATE", nullable: true),
                    FechaAtencionInicial = table.Column<DateOnly>(type: "DATE", nullable: true),
                    FechaCancelacion = table.Column<DateOnly>(type: "DATE", nullable: true),
                    FechaProgramacion = table.Column<DateOnly>(type: "DATE", nullable: true),
                    FechaRechazo = table.Column<DateOnly>(type: "DATE", nullable: true),
                    FechaSolicitud = table.Column<DateOnly>(type: "DATE", nullable: true),
                    HoraAceptacion = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    HoraAtencionFinal = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    HoraAtencionInicial = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    HoraCancelacion = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    HoraProgramacion = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    HoraRechazo = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    HoraSolicitud = table.Column<TimeOnly>(type: "TIME(0)", nullable: true),
                    IndicadorDestinoTipo = table.Column<bool>(type: "bit", nullable: false),
                    IndicadorOrigenTipo = table.Column<bool>(type: "bit", nullable: false),
                    IpOperador = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModalidadServicio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreSucursal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroBolsasMoneda = table.Column<int>(type: "int", nullable: true),
                    NumeroKitsCambio = table.Column<int>(type: "int", nullable: true),
                    NumeroPedido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperadorCancelacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperadorCgs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonaCancelacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PuntoDestino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PuntoOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsableFallido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SucursalCgs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoConcepto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoTraslado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValorBillete = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    ValorMoneda = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    ValorServicio = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true)
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

            migrationBuilder.AddForeignKey(
                name: "FK_CefTransacciones_AdmServicios_OrdenServicio",
                table: "CefTransacciones",
                column: "OrdenServicio",
                principalTable: "AdmServicios",
                principalColumn: "OrdenServicio",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
