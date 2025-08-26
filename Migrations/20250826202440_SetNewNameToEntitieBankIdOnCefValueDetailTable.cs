using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class SetNewNameToEntitieBankIdOnCefValueDetailTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefDetallesValores_AdmEntidadesBancarias_EntitieBankId",
                table: "CefDetallesValores");

            migrationBuilder.RenameColumn(
                name: "EntitieBankId",
                table: "CefDetallesValores",
                newName: "EntidadBancaria");

            migrationBuilder.RenameIndex(
                name: "IX_CefDetallesValores_EntitieBankId",
                table: "CefDetallesValores",
                newName: "IX_CefDetallesValores_EntidadBancaria");

            migrationBuilder.AddForeignKey(
                name: "FK_CefDetallesValores_AdmEntidadesBancarias_EntidadBancaria",
                table: "CefDetallesValores",
                column: "EntidadBancaria",
                principalTable: "AdmEntidadesBancarias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefDetallesValores_AdmEntidadesBancarias_EntidadBancaria",
                table: "CefDetallesValores");

            migrationBuilder.RenameColumn(
                name: "EntidadBancaria",
                table: "CefDetallesValores",
                newName: "EntitieBankId");

            migrationBuilder.RenameIndex(
                name: "IX_CefDetallesValores_EntidadBancaria",
                table: "CefDetallesValores",
                newName: "IX_CefDetallesValores_EntitieBankId");

            migrationBuilder.AddForeignKey(
                name: "FK_CefDetallesValores_AdmEntidadesBancarias_EntitieBankId",
                table: "CefDetallesValores",
                column: "EntitieBankId",
                principalTable: "AdmEntidadesBancarias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
