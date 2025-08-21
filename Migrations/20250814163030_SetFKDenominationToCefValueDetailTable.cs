using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class SetFKDenominationToCefValueDetailTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Denominacion",
                table: "CefDetallesValores",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,0)");

            migrationBuilder.CreateIndex(
                name: "IX_CefDetallesValores_Denominacion",
                table: "CefDetallesValores",
                column: "Denominacion");

            migrationBuilder.AddForeignKey(
                name: "FK_CefDetallesValores_AdmDenominaciones_Denominacion",
                table: "CefDetallesValores",
                column: "Denominacion",
                principalTable: "AdmDenominaciones",
                principalColumn: "CodDenominacion",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefDetallesValores_AdmDenominaciones_Denominacion",
                table: "CefDetallesValores");

            migrationBuilder.DropIndex(
                name: "IX_CefDetallesValores_Denominacion",
                table: "CefDetallesValores");

            migrationBuilder.AlterColumn<decimal>(
                name: "Denominacion",
                table: "CefDetallesValores",
                type: "DECIMAL(18,0)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
