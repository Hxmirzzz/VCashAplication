using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddInclusionLetterToAdmPointTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_CodCargo",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_CodCedula",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_CodSucursal",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_CodUnidad",
                table: "SegRegistroEmpleados");

            migrationBuilder.AlterColumn<string>(
                name: "PrimerApellidoEmpleado",
                table: "SegRegistroEmpleados",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CartaInclusion",
                table: "AdmPuntos",
                type: "nvarchar(455)",
                maxLength: 455,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_Cargo_Fecha",
                table: "SegRegistroEmpleados",
                columns: new[] { "CodCargo", "FechaEntrada" });

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_Empleado_Fecha",
                table: "SegRegistroEmpleados",
                columns: new[] { "CodCedula", "FechaEntrada" });

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_FechaEntrada",
                table: "SegRegistroEmpleados",
                column: "FechaEntrada");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_Indicadores_Fecha",
                table: "SegRegistroEmpleados",
                columns: new[] { "IndicadorEntrada", "IndicadorSalida", "FechaEntrada" });

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_PrimerApellido",
                table: "SegRegistroEmpleados",
                column: "PrimerApellidoEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_Sucursal_Fecha",
                table: "SegRegistroEmpleados",
                columns: new[] { "CodSucursal", "FechaEntrada" });

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_Unidad_Fecha",
                table: "SegRegistroEmpleados",
                columns: new[] { "CodUnidad", "FechaEntrada" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_RUTA_TipoAtencion",
                table: "AdmRutas",
                sql: "TipoAtencion IN ('AM','PM','AD')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RUTA_TipoRuta",
                table: "AdmRutas",
                sql: "TipoRuta IN ('T','A','M','L')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RUTA_TipoVehiculo",
                table: "AdmRutas",
                sql: "TipoVehiculo IN ('B','M','C','T')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_Cargo_Fecha",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_Empleado_Fecha",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_FechaEntrada",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_Indicadores_Fecha",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_PrimerApellido",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_Sucursal_Fecha",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropIndex(
                name: "IX_SegRegistroEmpleados_Unidad_Fecha",
                table: "SegRegistroEmpleados");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RUTA_TipoAtencion",
                table: "AdmRutas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RUTA_TipoRuta",
                table: "AdmRutas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RUTA_TipoVehiculo",
                table: "AdmRutas");

            migrationBuilder.DropColumn(
                name: "CartaInclusion",
                table: "AdmPuntos");

            migrationBuilder.AlterColumn<string>(
                name: "PrimerApellidoEmpleado",
                table: "SegRegistroEmpleados",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_CodCargo",
                table: "SegRegistroEmpleados",
                column: "CodCargo");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_CodCedula",
                table: "SegRegistroEmpleados",
                column: "CodCedula");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_CodSucursal",
                table: "SegRegistroEmpleados",
                column: "CodSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_SegRegistroEmpleados_CodUnidad",
                table: "SegRegistroEmpleados",
                column: "CodUnidad");
        }
    }
}
