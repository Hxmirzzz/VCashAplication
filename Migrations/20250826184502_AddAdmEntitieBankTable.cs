using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmEntitieBankTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntitieBankId",
                table: "CefDetallesValores",
                type: "nvarchar(10)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdmEntidadesBancarias",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmEntidadesBancarias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CefDetallesValores_EntitieBankId",
                table: "CefDetallesValores",
                column: "EntitieBankId");

            migrationBuilder.AddForeignKey(
                name: "FK_CefDetallesValores_AdmEntidadesBancarias_EntitieBankId",
                table: "CefDetallesValores",
                column: "EntitieBankId",
                principalTable: "AdmEntidadesBancarias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefDetallesValores_AdmEntidadesBancarias_EntitieBankId",
                table: "CefDetallesValores");

            migrationBuilder.DropTable(
                name: "AdmEntidadesBancarias");

            migrationBuilder.DropIndex(
                name: "IX_CefDetallesValores_EntitieBankId",
                table: "CefDetallesValores");

            migrationBuilder.DropColumn(
                name: "EntitieBankId",
                table: "CefDetallesValores");
        }
    }
}
