using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class RenameCefContenedoresTableToCefBolsasTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefContenedores_AspNetUsers_UsuarioProcesamientoId",
                table: "CefContenedores");

            migrationBuilder.DropForeignKey(
                name: "FK_CefContenedores_CefContenedores_IdContenedorPadre",
                table: "CefContenedores");

            migrationBuilder.DropForeignKey(
                name: "FK_CefContenedores_CefTransacciones_IdTransaccionCEF",
                table: "CefContenedores");

            migrationBuilder.DropForeignKey(
                name: "FK_CefDetallesValores_CefContenedores_IdContenedorCef",
                table: "CefDetallesValores");

            migrationBuilder.DropForeignKey(
                name: "FK_CefNovedades_CefContenedores_IdContenedorCef",
                table: "CefNovedades");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CefContenedores",
                table: "CefContenedores");

            migrationBuilder.RenameTable(
                name: "CefContenedores",
                newName: "CefBolsas");

            migrationBuilder.RenameIndex(
                name: "IX_CefContenedores_UsuarioProcesamientoId",
                table: "CefBolsas",
                newName: "IX_CefBolsas_UsuarioProcesamientoId");

            migrationBuilder.RenameIndex(
                name: "IX_CefContenedores_IdTransaccionCEF_CodigoContenedor",
                table: "CefBolsas",
                newName: "IX_CefBolsas_IdTransaccionCEF_CodigoContenedor");

            migrationBuilder.RenameIndex(
                name: "IX_CefContenedores_IdContenedorPadre",
                table: "CefBolsas",
                newName: "IX_CefBolsas_IdContenedorPadre");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CefBolsas",
                table: "CefBolsas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CefBolsas_AspNetUsers_UsuarioProcesamientoId",
                table: "CefBolsas",
                column: "UsuarioProcesamientoId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CefBolsas_CefBolsas_IdContenedorPadre",
                table: "CefBolsas",
                column: "IdContenedorPadre",
                principalTable: "CefBolsas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CefBolsas_CefTransacciones_IdTransaccionCEF",
                table: "CefBolsas",
                column: "IdTransaccionCEF",
                principalTable: "CefTransacciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CefDetallesValores_CefBolsas_IdContenedorCef",
                table: "CefDetallesValores",
                column: "IdContenedorCef",
                principalTable: "CefBolsas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CefNovedades_CefBolsas_IdContenedorCef",
                table: "CefNovedades",
                column: "IdContenedorCef",
                principalTable: "CefBolsas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefBolsas_AspNetUsers_UsuarioProcesamientoId",
                table: "CefBolsas");

            migrationBuilder.DropForeignKey(
                name: "FK_CefBolsas_CefBolsas_IdContenedorPadre",
                table: "CefBolsas");

            migrationBuilder.DropForeignKey(
                name: "FK_CefBolsas_CefTransacciones_IdTransaccionCEF",
                table: "CefBolsas");

            migrationBuilder.DropForeignKey(
                name: "FK_CefDetallesValores_CefBolsas_IdContenedorCef",
                table: "CefDetallesValores");

            migrationBuilder.DropForeignKey(
                name: "FK_CefNovedades_CefBolsas_IdContenedorCef",
                table: "CefNovedades");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CefBolsas",
                table: "CefBolsas");

            migrationBuilder.RenameTable(
                name: "CefBolsas",
                newName: "CefContenedores");

            migrationBuilder.RenameIndex(
                name: "IX_CefBolsas_UsuarioProcesamientoId",
                table: "CefContenedores",
                newName: "IX_CefContenedores_UsuarioProcesamientoId");

            migrationBuilder.RenameIndex(
                name: "IX_CefBolsas_IdTransaccionCEF_CodigoContenedor",
                table: "CefContenedores",
                newName: "IX_CefContenedores_IdTransaccionCEF_CodigoContenedor");

            migrationBuilder.RenameIndex(
                name: "IX_CefBolsas_IdContenedorPadre",
                table: "CefContenedores",
                newName: "IX_CefContenedores_IdContenedorPadre");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CefContenedores",
                table: "CefContenedores",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CefContenedores_AspNetUsers_UsuarioProcesamientoId",
                table: "CefContenedores",
                column: "UsuarioProcesamientoId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CefContenedores_CefContenedores_IdContenedorPadre",
                table: "CefContenedores",
                column: "IdContenedorPadre",
                principalTable: "CefContenedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CefContenedores_CefTransacciones_IdTransaccionCEF",
                table: "CefContenedores",
                column: "IdTransaccionCEF",
                principalTable: "CefTransacciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CefDetallesValores_CefContenedores_IdContenedorCef",
                table: "CefDetallesValores",
                column: "IdContenedorCef",
                principalTable: "CefContenedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CefNovedades_CefContenedores_IdContenedorCef",
                table: "CefNovedades",
                column: "IdContenedorCef",
                principalTable: "CefContenedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
