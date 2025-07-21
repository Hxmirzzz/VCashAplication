using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileSegRegistroEmpleadoMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "HoraSalida",
                table: "SegRegistroEmpleados",
                type: "TIME(0)",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "HoraEntrada",
                table: "SegRegistroEmpleados",
                type: "TIME(0)",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FechaSalida",
                table: "SegRegistroEmpleados",
                type: "DATE",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FechaEntrada",
                table: "SegRegistroEmpleados",
                type: "DATE",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "HoraSalida",
                table: "SegRegistroEmpleados",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "TIME(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "HoraEntrada",
                table: "SegRegistroEmpleados",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "TIME(0)");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FechaSalida",
                table: "SegRegistroEmpleados",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "DATE",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FechaEntrada",
                table: "SegRegistroEmpleados",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "DATE");
        }
    }
}
