using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCefModuleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CefTiposNovedad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AplicaPara = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CefTiposNovedad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CefTransacciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdenServicio = table.Column<string>(type: "NVARCHAR(450)", maxLength: 450, nullable: false),
                    CodRuta = table.Column<string>(type: "VARCHAR(12)", maxLength: 12, nullable: false),
                    NumeroPlanilla = table.Column<int>(type: "int", nullable: false),
                    Divisa = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TipoTransaccion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroMesaConteo = table.Column<int>(type: "int", nullable: true),
                    CantidadBolsasDeclaradas = table.Column<int>(type: "int", nullable: false),
                    CantidadSobresDeclarados = table.Column<int>(type: "int", nullable: false),
                    CantidadChequesDeclarados = table.Column<int>(type: "int", nullable: false),
                    CantidadDocumentosDeclarados = table.Column<int>(type: "int", nullable: false),
                    ValorBilletesDeclarado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    ValorMonedasDeclarado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    ValorDocumentosDeclarado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    ValorTotalDeclarado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    ValorTotalDeclaradoLetras = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ValorTotalContado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    ValorTotalContadoLetras = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DiferenciaValor = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    NovedadInformativa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EsCustodia = table.Column<bool>(type: "bit", nullable: false),
                    EsPuntoAPunto = table.Column<bool>(type: "bit", nullable: false),
                    EstadoTransaccion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    UsuarioRegistroId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FechaInicioConteo = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    FechaFinConteo = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    UsuarioConteoBilletesId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UsuarioConteoMonedasId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UsuarioRevisorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UsuarioBovedaId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FechaUltimaActualizacion = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    UsuarioUltimaActualizacionId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IPRegistro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CefTransacciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CefTransacciones_AdmServicios_OrdenServicio",
                        column: x => x.OrdenServicio,
                        principalTable: "AdmServicios",
                        principalColumn: "OrdenServicio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefTransacciones_AspNetUsers_UsuarioBovedaId",
                        column: x => x.UsuarioBovedaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefTransacciones_AspNetUsers_UsuarioConteoBilletesId",
                        column: x => x.UsuarioConteoBilletesId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefTransacciones_AspNetUsers_UsuarioConteoMonedasId",
                        column: x => x.UsuarioConteoMonedasId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefTransacciones_AspNetUsers_UsuarioRegistroId",
                        column: x => x.UsuarioRegistroId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefTransacciones_AspNetUsers_UsuarioRevisorId",
                        column: x => x.UsuarioRevisorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefTransacciones_AspNetUsers_UsuarioUltimaActualizacionId",
                        column: x => x.UsuarioUltimaActualizacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefTransacciones_TdvRutasDiarias_CodRuta",
                        column: x => x.CodRuta,
                        principalTable: "TdvRutasDiarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CgsTiposUbicacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreTipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CgsTiposUbicacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CefContenedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTransaccionCEF = table.Column<int>(type: "int", nullable: false),
                    IdContenedorPadre = table.Column<int>(type: "int", nullable: true),
                    TipoContenedor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodigoContenedor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValorDeclarado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    ValorContado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    EstadoContenedor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UsuarioProcesamientoId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FechaProcesamiento = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    IdCajeroCliente = table.Column<int>(type: "INT", nullable: true),
                    NombreCajeroCliente = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FechaSobreCliente = table.Column<DateOnly>(type: "DATE", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CefContenedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CefContenedores_AspNetUsers_UsuarioProcesamientoId",
                        column: x => x.UsuarioProcesamientoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefContenedores_CefContenedores_IdContenedorPadre",
                        column: x => x.IdContenedorPadre,
                        principalTable: "CefContenedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefContenedores_CefTransacciones_IdTransaccionCEF",
                        column: x => x.IdTransaccionCEF,
                        principalTable: "CefTransacciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CefDetallesValores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdContenedorCef = table.Column<int>(type: "int", nullable: false),
                    TipoValor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Denominacion = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    CantidadFajos = table.Column<int>(type: "int", nullable: true),
                    CantidadPicos = table.Column<int>(type: "int", nullable: true),
                    ValorUnitario = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: true),
                    MontoCalculado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    EsAltaDenominacion = table.Column<bool>(type: "bit", nullable: true),
                    NumeroIdentificador = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Banco = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaEmision = table.Column<DateOnly>(type: "DATE", nullable: true),
                    Emisor = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CefDetallesValores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CefDetallesValores_CefContenedores_IdContenedorCef",
                        column: x => x.IdContenedorCef,
                        principalTable: "CefContenedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CefNovedades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTransaccionCef = table.Column<int>(type: "int", nullable: true),
                    IdContenedorCef = table.Column<int>(type: "int", nullable: true),
                    IdDetalleValorCef = table.Column<int>(type: "int", nullable: true),
                    IdTipoNovedad = table.Column<int>(type: "int", nullable: false),
                    MontoAfectado = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    DenominacionAfectada = table.Column<decimal>(type: "DECIMAL(18,0)", nullable: false),
                    CantidadAfectada = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UsuarioReportaId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FechaNovedad = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    EstadoNovedad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CefNovedades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CefNovedades_AspNetUsers_UsuarioReportaId",
                        column: x => x.UsuarioReportaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefNovedades_CefContenedores_IdContenedorCef",
                        column: x => x.IdContenedorCef,
                        principalTable: "CefContenedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefNovedades_CefDetallesValores_IdDetalleValorCef",
                        column: x => x.IdDetalleValorCef,
                        principalTable: "CefDetallesValores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefNovedades_CefTiposNovedad_IdTipoNovedad",
                        column: x => x.IdTipoNovedad,
                        principalTable: "CefTiposNovedad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CefNovedades_CefTransacciones_IdTransaccionCef",
                        column: x => x.IdTransaccionCef,
                        principalTable: "CefTransacciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CefContenedores_IdContenedorPadre",
                table: "CefContenedores",
                column: "IdContenedorPadre");

            migrationBuilder.CreateIndex(
                name: "IX_CefContenedores_IdTransaccionCEF",
                table: "CefContenedores",
                column: "IdTransaccionCEF");

            migrationBuilder.CreateIndex(
                name: "IX_CefContenedores_UsuarioProcesamientoId",
                table: "CefContenedores",
                column: "UsuarioProcesamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_CefDetallesValores_IdContenedorCef",
                table: "CefDetallesValores",
                column: "IdContenedorCef");

            migrationBuilder.CreateIndex(
                name: "IX_CefNovedades_IdContenedorCef",
                table: "CefNovedades",
                column: "IdContenedorCef");

            migrationBuilder.CreateIndex(
                name: "IX_CefNovedades_IdDetalleValorCef",
                table: "CefNovedades",
                column: "IdDetalleValorCef");

            migrationBuilder.CreateIndex(
                name: "IX_CefNovedades_IdTipoNovedad",
                table: "CefNovedades",
                column: "IdTipoNovedad");

            migrationBuilder.CreateIndex(
                name: "IX_CefNovedades_IdTransaccionCef",
                table: "CefNovedades",
                column: "IdTransaccionCef");

            migrationBuilder.CreateIndex(
                name: "IX_CefNovedades_UsuarioReportaId",
                table: "CefNovedades",
                column: "UsuarioReportaId");

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_CodRuta",
                table: "CefTransacciones",
                column: "CodRuta");

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_OrdenServicio",
                table: "CefTransacciones",
                column: "OrdenServicio");

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_UsuarioBovedaId",
                table: "CefTransacciones",
                column: "UsuarioBovedaId");

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_UsuarioConteoBilletesId",
                table: "CefTransacciones",
                column: "UsuarioConteoBilletesId");

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_UsuarioConteoMonedasId",
                table: "CefTransacciones",
                column: "UsuarioConteoMonedasId");

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_UsuarioRegistroId",
                table: "CefTransacciones",
                column: "UsuarioRegistroId");

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_UsuarioRevisorId",
                table: "CefTransacciones",
                column: "UsuarioRevisorId");

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_UsuarioUltimaActualizacionId",
                table: "CefTransacciones",
                column: "UsuarioUltimaActualizacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CefNovedades");

            migrationBuilder.DropTable(
                name: "CgsTiposUbicacion");

            migrationBuilder.DropTable(
                name: "CefDetallesValores");

            migrationBuilder.DropTable(
                name: "CefTiposNovedad");

            migrationBuilder.DropTable(
                name: "CefContenedores");

            migrationBuilder.DropTable(
                name: "CefTransacciones");
        }
    }
}
