using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountNumberAndCheckNumberToCefValueDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumeroCheque",
                table: "CefDetallesValores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroCuenta",
                table: "CefDetallesValores",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroCheque",
                table: "CefDetallesValores");

            migrationBuilder.DropColumn(
                name: "NumeroCuenta",
                table: "CefDetallesValores");
        }
    }
}
