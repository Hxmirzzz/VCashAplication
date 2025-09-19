using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class SetNewColumnDenominationIdToCefIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DenominacionAfectada",
                table: "CefNovedades",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,0)");

            migrationBuilder.CreateIndex(
                name: "IX_CefNovedades_DenominacionAfectada",
                table: "CefNovedades",
                column: "DenominacionAfectada");

            migrationBuilder.AddForeignKey(
                name: "FK_CefNovedades_AdmDenominaciones_DenominacionAfectada",
                table: "CefNovedades",
                column: "DenominacionAfectada",
                principalTable: "AdmDenominaciones",
                principalColumn: "CodDenominacion",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefNovedades_AdmDenominaciones_DenominacionAfectada",
                table: "CefNovedades");

            migrationBuilder.DropIndex(
                name: "IX_CefNovedades_DenominacionAfectada",
                table: "CefNovedades");

            migrationBuilder.AlterColumn<decimal>(
                name: "DenominacionAfectada",
                table: "CefNovedades",
                type: "DECIMAL(18,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
