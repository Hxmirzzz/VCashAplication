using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCashValuesToCefTransactionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValorBilletesAltaContado",
                table: "CefTransacciones",
                type: "DECIMAL(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorBilletesBajaContado",
                table: "CefTransacciones",
                type: "DECIMAL(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorBilletesContado",
                table: "CefTransacciones",
                type: "DECIMAL(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorChequesContado",
                table: "CefTransacciones",
                type: "DECIMAL(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDocumentosContado",
                table: "CefTransacciones",
                type: "DECIMAL(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorMonedasContado",
                table: "CefTransacciones",
                type: "DECIMAL(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorTotalGeneral",
                table: "CefTransacciones",
                type: "DECIMAL(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ValorTotalGeneralLetras",
                table: "CefTransacciones",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValorBilletesAltaContado",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "ValorBilletesBajaContado",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "ValorBilletesContado",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "ValorChequesContado",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "ValorDocumentosContado",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "ValorMonedasContado",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "ValorTotalGeneral",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "ValorTotalGeneralLetras",
                table: "CefTransacciones");
        }
    }
}
