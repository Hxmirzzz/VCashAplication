using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class DeleteStatusTableForRefatorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmEstados",
                columns: table => new
                {
                    CodEstado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreEstado = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmEstados", x => x.CodEstado);
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
                principalColumn: "CodEstado");
        }
    }
}
