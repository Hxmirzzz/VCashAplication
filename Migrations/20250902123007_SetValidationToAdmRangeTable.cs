using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class SetValidationToAdmRangeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmRangos_CodCliente",
                table: "AdmRangos");

            migrationBuilder.DropColumn(
                name: "Concatenado",
                table: "AdmRangos");

            migrationBuilder.AddColumn<string>(
                name: "schedule_key",
                table: "AdmRangos",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "(IIF([Lunes]=1,'1','0')+':'+ISNULL(LEFT(CONVERT(CHAR(8), [Lr1Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Lr1Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Lr2Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Lr2Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Lr3Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Lr3Hf], 108), 5), '00:00')+'|'+IIF([Martes]=1,'1','0')+':'+ISNULL(LEFT(CONVERT(CHAR(8), [Mr1Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Mr1Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Mr2Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Mr2Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Mr3Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Mr3Hf], 108), 5), '00:00')+'|'+IIF([Miercoles]=1,'1','0')+':'+ISNULL(LEFT(CONVERT(CHAR(8), [Wr1Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Wr1Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Wr2Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Wr2Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Wr3Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Wr3Hf], 108), 5), '00:00')+'|'+IIF([Jueves]=1,'1','0')+':'+ISNULL(LEFT(CONVERT(CHAR(8), [Jr1Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Jr1Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Jr2Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Jr2Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Jr3Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Jr3Hf], 108), 5), '00:00')+'|'+IIF([Viernes]=1,'1','0')+':'+ISNULL(LEFT(CONVERT(CHAR(8), [Vr1Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Vr1Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Vr2Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Vr2Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Vr3Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Vr3Hf], 108), 5), '00:00')+'|'+IIF([Sabado]=1,'1','0')+':'+ISNULL(LEFT(CONVERT(CHAR(8), [Sr1Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Sr1Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Sr2Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Sr2Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Sr3Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Sr3Hf], 108), 5), '00:00')+'|'+IIF([Domingo]=1,'1','0')+':'+ISNULL(LEFT(CONVERT(CHAR(8), [Dr1Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Dr1Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Dr2Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Dr2Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Dr3Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Dr3Hf], 108), 5), '00:00')+'|'+IIF([Festivo]=1,'1','0')+':'+ISNULL(LEFT(CONVERT(CHAR(8), [Fr1Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Fr1Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Fr2Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Fr2Hf], 108), 5), '00:00')+','+ISNULL(LEFT(CONVERT(CHAR(8), [Fr3Hi], 108), 5), '00:00')+'-'+ISNULL(LEFT(CONVERT(CHAR(8), [Fr3Hf], 108), 5), '00:00'))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "UX_AdmRangos_Cliente_Schedule",
                table: "AdmRangos",
                columns: new[] { "CodCliente", "schedule_key" },
                unique: true,
                filter: "[RangeStatus] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AdmRangos_Cliente_Schedule",
                table: "AdmRangos");

            migrationBuilder.DropColumn(
                name: "schedule_key",
                table: "AdmRangos");

            migrationBuilder.AddColumn<string>(
                name: "Concatenado",
                table: "AdmRangos",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AdmRangos_CodCliente",
                table: "AdmRangos",
                column: "CodCliente");
        }
    }
}
