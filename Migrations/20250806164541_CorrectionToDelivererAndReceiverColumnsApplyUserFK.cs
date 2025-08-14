using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class CorrectionToDelivererAndReceiverColumnsApplyUserFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Entrega",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "Recibe",
                table: "CefTransacciones");

            migrationBuilder.AddColumn<string>(
                name: "ReponsableEntregaId",
                table: "CefTransacciones",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsableRecibeId",
                table: "CefTransacciones",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReponsableEntregaId",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "ResponsableRecibeId",
                table: "CefTransacciones");

            migrationBuilder.AddColumn<string>(
                name: "Entrega",
                table: "CefTransacciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recibe",
                table: "CefTransacciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
