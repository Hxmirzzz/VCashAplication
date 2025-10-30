using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmEmpleados_CodSucursal",
                table: "AdmEmpleados");

            migrationBuilder.AlterColumn<string>(
                name: "SegundoApellido",
                table: "AdmEmpleados",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimerNombre",
                table: "AdmEmpleados",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCarnet",
                table: "AdmEmpleados",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompleto",
                table: "AdmEmpleados",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Genero",
                table: "AdmEmpleados",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmEmpleados_Filters",
                table: "AdmEmpleados",
                columns: new[] { "CodSucursal", "CodCargo", "EmpleadoEstado", "Genero" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmEmpleados_NombreCompleto",
                table: "AdmEmpleados",
                column: "NombreCompleto");

            migrationBuilder.CreateIndex(
                name: "IX_AdmEmpleados_NumeroCarnet",
                table: "AdmEmpleados",
                column: "NumeroCarnet");

            migrationBuilder.CreateIndex(
                name: "IX_AdmEmpleados_Sorting",
                table: "AdmEmpleados",
                columns: new[] { "FecVinculacion", "SegundoApellido", "PrimerNombre" },
                descending: new[] { true, false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmEmpleados_Filters",
                table: "AdmEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_AdmEmpleados_NombreCompleto",
                table: "AdmEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_AdmEmpleados_NumeroCarnet",
                table: "AdmEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_AdmEmpleados_Sorting",
                table: "AdmEmpleados");

            migrationBuilder.AlterColumn<string>(
                name: "SegundoApellido",
                table: "AdmEmpleados",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimerNombre",
                table: "AdmEmpleados",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCarnet",
                table: "AdmEmpleados",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompleto",
                table: "AdmEmpleados",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Genero",
                table: "AdmEmpleados",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmEmpleados_CodSucursal",
                table: "AdmEmpleados",
                column: "CodSucursal");
        }
    }
}
