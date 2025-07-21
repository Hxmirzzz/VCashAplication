using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class DropColumnUsuarioRegistroId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmEmpleados_AspNetUsers_UsuarioRegistroId",
                table: "AdmEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_AdmEmpleados_UsuarioRegistroId",
                table: "AdmEmpleados");

            migrationBuilder.DropColumn(
                name: "UsuarioRegistroId",
                table: "AdmEmpleados");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioRegistroId",
                table: "AdmEmpleados",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmEmpleados_UsuarioRegistroId",
                table: "AdmEmpleados",
                column: "UsuarioRegistroId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmEmpleados_AspNetUsers_UsuarioRegistroId",
                table: "AdmEmpleados",
                column: "UsuarioRegistroId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
