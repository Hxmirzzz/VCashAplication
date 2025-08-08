using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDelivererAndReceiverColumnToCefTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Entrega",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "Recibe",
                table: "CefTransacciones");
        }
    }
}
