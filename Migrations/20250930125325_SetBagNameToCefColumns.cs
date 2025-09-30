using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class SetBagNameToCefColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefBolsas_CefBolsas_IdContenedorPadre",
                table: "CefBolsas");

            migrationBuilder.DropForeignKey(
                name: "FK_CefDetallesValores_CefBolsas_IdContenedorCef",
                table: "CefDetallesValores");

            migrationBuilder.DropForeignKey(
                name: "FK_CefNovedades_CefBolsas_IdContenedorCef",
                table: "CefNovedades");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CEF_SOBRE_Padre",
                table: "CefBolsas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CEF_SOBRE_TipoSobreValido",
                table: "CefBolsas");

            migrationBuilder.RenameColumn(
                name: "IdContenedorCef",
                table: "CefNovedades",
                newName: "IdBolsaCef");

            migrationBuilder.RenameIndex(
                name: "IX_CefNovedades_IdContenedorCef",
                table: "CefNovedades",
                newName: "IX_CefNovedades_IdBolsaCef");

            migrationBuilder.RenameColumn(
                name: "IdContenedorCef",
                table: "CefDetallesValores",
                newName: "IdBolsaCef");

            migrationBuilder.RenameIndex(
                name: "IX_CefDetallesValores_IdContenedorCef_TipoValor_Denominacion_Calidad",
                table: "CefDetallesValores",
                newName: "IX_CefDetallesValores_IdBolsaCef_TipoValor_Denominacion_Calidad");

            migrationBuilder.RenameColumn(
                name: "TipoContenedor",
                table: "CefBolsas",
                newName: "TipoBolsa");

            migrationBuilder.RenameColumn(
                name: "IdContenedorPadre",
                table: "CefBolsas",
                newName: "IdBolsaPadre");

            migrationBuilder.RenameColumn(
                name: "EstadoContenedor",
                table: "CefBolsas",
                newName: "EstadoBolsa");

            migrationBuilder.RenameColumn(
                name: "CodigoContenedor",
                table: "CefBolsas",
                newName: "CodigoBolsa");

            migrationBuilder.RenameIndex(
                name: "IX_CefBolsas_IdTransaccionCEF_CodigoContenedor",
                table: "CefBolsas",
                newName: "IX_CefBolsas_IdTransaccionCEF_CodigoBolsa");

            migrationBuilder.RenameIndex(
                name: "IX_CefBolsas_IdContenedorPadre",
                table: "CefBolsas",
                newName: "IX_CefBolsas_IdBolsaPadre");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CEF_SOBRE_Padre",
                table: "CefBolsas",
                sql: "(([TipoBolsa] = 'Sobre' AND [IdBolsaPadre] IS NOT NULL) OR  ([TipoBolsa] <> 'Sobre' AND [IdBolsaPadre] IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CEF_SOBRE_TipoSobreValido",
                table: "CefBolsas",
                sql: "([TipoBolsa] <> 'Sobre') OR ([TipoSobre] IN ('Efectivo','Documento','Cheque'))");

            migrationBuilder.AddForeignKey(
                name: "FK_CefBolsas_CefBolsas_IdBolsaPadre",
                table: "CefBolsas",
                column: "IdBolsaPadre",
                principalTable: "CefBolsas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CefDetallesValores_CefBolsas_IdBolsaCef",
                table: "CefDetallesValores",
                column: "IdBolsaCef",
                principalTable: "CefBolsas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CefNovedades_CefBolsas_IdBolsaCef",
                table: "CefNovedades",
                column: "IdBolsaCef",
                principalTable: "CefBolsas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CefBolsas_CefBolsas_IdBolsaPadre",
                table: "CefBolsas");

            migrationBuilder.DropForeignKey(
                name: "FK_CefDetallesValores_CefBolsas_IdBolsaCef",
                table: "CefDetallesValores");

            migrationBuilder.DropForeignKey(
                name: "FK_CefNovedades_CefBolsas_IdBolsaCef",
                table: "CefNovedades");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CEF_SOBRE_Padre",
                table: "CefBolsas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CEF_SOBRE_TipoSobreValido",
                table: "CefBolsas");

            migrationBuilder.RenameColumn(
                name: "IdBolsaCef",
                table: "CefNovedades",
                newName: "IdContenedorCef");

            migrationBuilder.RenameIndex(
                name: "IX_CefNovedades_IdBolsaCef",
                table: "CefNovedades",
                newName: "IX_CefNovedades_IdContenedorCef");

            migrationBuilder.RenameColumn(
                name: "IdBolsaCef",
                table: "CefDetallesValores",
                newName: "IdContenedorCef");

            migrationBuilder.RenameIndex(
                name: "IX_CefDetallesValores_IdBolsaCef_TipoValor_Denominacion_Calidad",
                table: "CefDetallesValores",
                newName: "IX_CefDetallesValores_IdContenedorCef_TipoValor_Denominacion_Calidad");

            migrationBuilder.RenameColumn(
                name: "TipoBolsa",
                table: "CefBolsas",
                newName: "TipoContenedor");

            migrationBuilder.RenameColumn(
                name: "IdBolsaPadre",
                table: "CefBolsas",
                newName: "IdContenedorPadre");

            migrationBuilder.RenameColumn(
                name: "EstadoBolsa",
                table: "CefBolsas",
                newName: "EstadoContenedor");

            migrationBuilder.RenameColumn(
                name: "CodigoBolsa",
                table: "CefBolsas",
                newName: "CodigoContenedor");

            migrationBuilder.RenameIndex(
                name: "IX_CefBolsas_IdTransaccionCEF_CodigoBolsa",
                table: "CefBolsas",
                newName: "IX_CefBolsas_IdTransaccionCEF_CodigoContenedor");

            migrationBuilder.RenameIndex(
                name: "IX_CefBolsas_IdBolsaPadre",
                table: "CefBolsas",
                newName: "IX_CefBolsas_IdContenedorPadre");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CEF_SOBRE_Padre",
                table: "CefBolsas",
                sql: "(([TipoContenedor] = 'Sobre' AND [IdContenedorPadre] IS NOT NULL) OR  ([TipoContenedor] <> 'Sobre' AND [IdContenedorPadre] IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CEF_SOBRE_TipoSobreValido",
                table: "CefBolsas",
                sql: "([TipoContenedor] <> 'Sobre') OR ([TipoSobre] IN ('Efectivo','Documento','Cheque'))");

            migrationBuilder.AddForeignKey(
                name: "FK_CefBolsas_CefBolsas_IdContenedorPadre",
                table: "CefBolsas",
                column: "IdContenedorPadre",
                principalTable: "CefBolsas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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
    }
}
