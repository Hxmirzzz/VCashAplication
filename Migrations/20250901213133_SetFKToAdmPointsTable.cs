using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class SetFKToAdmPointsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CodigoRango",
                table: "AdmPuntos",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmPuntos_CodigoRango",
                table: "AdmPuntos",
                column: "CodigoRango");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmPuntos_AdmRangos_CodigoRango",
                table: "AdmPuntos",
                column: "CodigoRango",
                principalTable: "AdmRangos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmPuntos_AdmRangos_CodigoRango",
                table: "AdmPuntos");

            migrationBuilder.DropIndex(
                name: "IX_AdmPuntos_CodigoRango",
                table: "AdmPuntos");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoRango",
                table: "AdmPuntos",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
