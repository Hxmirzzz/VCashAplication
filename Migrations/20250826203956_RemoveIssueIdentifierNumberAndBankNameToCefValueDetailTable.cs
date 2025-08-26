using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIssueIdentifierNumberAndBankNameToCefValueDetailTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banco",
                table: "CefDetallesValores");

            migrationBuilder.DropColumn(
                name: "Emisor",
                table: "CefDetallesValores");

            migrationBuilder.DropColumn(
                name: "NumeroIdentificador",
                table: "CefDetallesValores");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Banco",
                table: "CefDetallesValores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Emisor",
                table: "CefDetallesValores",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroIdentificador",
                table: "CefDetallesValores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
