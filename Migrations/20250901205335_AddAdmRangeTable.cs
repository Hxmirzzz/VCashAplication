using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmRangeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmRangos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodRango = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodCliente = table.Column<int>(type: "int", nullable: false),
                    InformacionRango = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Lunes = table.Column<bool>(type: "bit", nullable: false),
                    CodLunes = table.Column<int>(type: "int", nullable: false),
                    Lr1Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Lr1Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Lr2Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Lr2Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Lr3Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Lr3Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Martes = table.Column<bool>(type: "bit", nullable: false),
                    CodMartes = table.Column<int>(type: "int", nullable: false),
                    Mr1Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Mr1Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Mr2Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Mr2Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Mr3Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Mr3Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Miercoles = table.Column<bool>(type: "bit", nullable: false),
                    CodMiercoles = table.Column<int>(type: "int", nullable: false),
                    Wr1Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Wr1Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Wr2Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Wr2Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Wr3Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Wr3Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Jueves = table.Column<bool>(type: "bit", nullable: false),
                    CodJueves = table.Column<int>(type: "int", nullable: false),
                    Jr1Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Jr1Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Jr2Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Jr2Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Jr3Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Jr3Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Viernes = table.Column<bool>(type: "bit", nullable: false),
                    CodViernes = table.Column<int>(type: "int", nullable: false),
                    Vr1Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Vr1Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Vr2Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Vr2Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Vr3Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Vr3Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Sabado = table.Column<bool>(type: "bit", nullable: false),
                    CodSabado = table.Column<int>(type: "int", nullable: false),
                    Sr1Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Sr1Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Sr2Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Sr2Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Sr3Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Sr3Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Domingo = table.Column<bool>(type: "bit", nullable: false),
                    CodDomingo = table.Column<int>(type: "int", nullable: false),
                    Dr1Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Dr1Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Dr2Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Dr2Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Dr3Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Dr3Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Festivo = table.Column<bool>(type: "bit", nullable: false),
                    CodFestivo = table.Column<int>(type: "int", nullable: false),
                    Fr1Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Fr1Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Fr2Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Fr2Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Fr3Hi = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Fr3Hf = table.Column<TimeSpan>(type: "TIME(0)", nullable: true),
                    Concatenado = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RangeStatus = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmRangos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmRangos_AdmClientes_CodCliente",
                        column: x => x.CodCliente,
                        principalTable: "AdmClientes",
                        principalColumn: "CodigoCliente",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmRangos_CodCliente",
                table: "AdmRangos",
                column: "CodCliente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmRangos");
        }
    }
}
