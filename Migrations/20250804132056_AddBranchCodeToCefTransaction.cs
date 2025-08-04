using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchCodeToCefTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CodSucursal",
                table: "CefTransacciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_CodSucursal",
                table: "CefTransacciones",
                column: "CodSucursal");

            migrationBuilder.AddForeignKey(
                name: "FK_CefTransacciones_AdmSucursales_CodSucursal",
                table: "CefTransacciones",
                column: "CodSucursal",
                principalTable: "AdmSucursales",
                principalColumn: "CodSucursal",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefTransacciones_AdmSucursales_CodSucursal",
                table: "CefTransacciones");

            migrationBuilder.DropIndex(
                name: "IX_CefTransacciones_CodSucursal",
                table: "CefTransacciones");

            migrationBuilder.DropColumn(
                name: "CodSucursal",
                table: "CefTransacciones");
        }
    }
}
