using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTypeValueColumnToAdmDenominations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoValor",
                table: "AdmDenominaciones");

            migrationBuilder.AddColumn<bool>(
                name: "AltaDenominacion",
                table: "AdmDenominaciones",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AltaDenominacion",
                table: "AdmDenominaciones");

            migrationBuilder.AddColumn<string>(
                name: "TipoValor",
                table: "AdmDenominaciones",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
