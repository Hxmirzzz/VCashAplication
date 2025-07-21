using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySheetsColumnsToDailyRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CantPlanillaEntrega",
                table: "TdvRutasDiarias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CantPlanillaRecibe",
                table: "TdvRutasDiarias",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantPlanillaEntrega",
                table: "TdvRutasDiarias");

            migrationBuilder.DropColumn(
                name: "CantPlanillaRecibe",
                table: "TdvRutasDiarias");
        }
    }
}
