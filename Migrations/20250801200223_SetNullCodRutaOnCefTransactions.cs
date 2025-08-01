using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class SetNullCodRutaOnCefTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CodRuta",
                table: "CefTransacciones",
                type: "VARCHAR(12)",
                maxLength: 12,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR(12)",
                oldMaxLength: 12);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CodRuta",
                table: "CefTransacciones",
                type: "VARCHAR(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "VARCHAR(12)",
                oldMaxLength: 12,
                oldNullable: true);
        }
    }
}
