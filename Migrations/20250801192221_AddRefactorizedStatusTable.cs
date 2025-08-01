using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRefactorizedStatusTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmEstados",
                columns: table => new
                {
                    CodigoEstado = table.Column<int>(type: "int", nullable: false),
                    NombreEstado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmEstados", x => x.CodigoEstado);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CgsServicios_CodEstado",
                table: "CgsServicios",
                column: "CodEstado");

            migrationBuilder.AddForeignKey(
                name: "FK_CgsServicios_AdmEstados_CodEstado",
                table: "CgsServicios",
                column: "CodEstado",
                principalTable: "AdmEstados",
                principalColumn: "CodigoEstado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CgsServicios_AdmEstados_CodEstado",
                table: "CgsServicios");

            migrationBuilder.DropTable(
                name: "AdmEstados");

            migrationBuilder.DropIndex(
                name: "IX_CgsServicios_CodEstado",
                table: "CgsServicios");
        }
    }
}
