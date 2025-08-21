using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmQualityTableAndFKToCefValueDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CefDetallesValores_IdContenedorCef",
                table: "CefDetallesValores");

            migrationBuilder.AddColumn<int>(
                name: "Calidad",
                table: "CefDetallesValores",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdmCalidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreCalidad = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TipoDinero = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Familia = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmCalidad", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CefDetallesValores_Calidad",
                table: "CefDetallesValores",
                column: "Calidad");

            migrationBuilder.CreateIndex(
                name: "IX_CefDetallesValores_IdContenedorCef_TipoValor_Denominacion_Calidad",
                table: "CefDetallesValores",
                columns: new[] { "IdContenedorCef", "TipoValor", "Denominacion", "Calidad" });

            migrationBuilder.AddForeignKey(
                name: "FK_CefDetallesValores_AdmCalidad_Calidad",
                table: "CefDetallesValores",
                column: "Calidad",
                principalTable: "AdmCalidad",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefDetallesValores_AdmCalidad_Calidad",
                table: "CefDetallesValores");

            migrationBuilder.DropTable(
                name: "AdmCalidad");

            migrationBuilder.DropIndex(
                name: "IX_CefDetallesValores_Calidad",
                table: "CefDetallesValores");

            migrationBuilder.DropIndex(
                name: "IX_CefDetallesValores_IdContenedorCef_TipoValor_Denominacion_Calidad",
                table: "CefDetallesValores");

            migrationBuilder.DropColumn(
                name: "Calidad",
                table: "CefDetallesValores");

            migrationBuilder.CreateIndex(
                name: "IX_CefDetallesValores_IdContenedorCef",
                table: "CefDetallesValores",
                column: "IdContenedorCef");
        }
    }
}
