using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class SetFKToReceiverAndDelivered : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_ReponsableEntregaId",
                table: "CefTransacciones",
                column: "ReponsableEntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_CefTransacciones_ResponsableRecibeId",
                table: "CefTransacciones",
                column: "ResponsableRecibeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CefTransacciones_AspNetUsers_ReponsableEntregaId",
                table: "CefTransacciones",
                column: "ReponsableEntregaId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CefTransacciones_AspNetUsers_ResponsableRecibeId",
                table: "CefTransacciones",
                column: "ResponsableRecibeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefTransacciones_AspNetUsers_ReponsableEntregaId",
                table: "CefTransacciones");

            migrationBuilder.DropForeignKey(
                name: "FK_CefTransacciones_AspNetUsers_ResponsableRecibeId",
                table: "CefTransacciones");

            migrationBuilder.DropIndex(
                name: "IX_CefTransacciones_ReponsableEntregaId",
                table: "CefTransacciones");

            migrationBuilder.DropIndex(
                name: "IX_CefTransacciones_ResponsableRecibeId",
                table: "CefTransacciones");
        }
    }
}
