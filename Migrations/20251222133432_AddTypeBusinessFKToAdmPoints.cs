using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeBusinessFKToAdmPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AdmPuntos_TipoNegocio",
                table: "AdmPuntos",
                column: "TipoNegocio");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmPuntos_AdmTypeBusinesses_TipoNegocio",
                table: "AdmPuntos",
                column: "TipoNegocio",
                principalTable: "AdmTypeBusinesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmPuntos_AdmTypeBusinesses_TipoNegocio",
                table: "AdmPuntos");

            migrationBuilder.DropIndex(
                name: "IX_AdmPuntos_TipoNegocio",
                table: "AdmPuntos");
        }
    }
}
