using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCashApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopeSubTypeColumnToCefContainerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CefContenedores_IdTransaccionCEF",
                table: "CefContenedores");

            migrationBuilder.AddColumn<string>(
                name: "TipoSobre",
                table: "CefContenedores",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CefContenedores_IdTransaccionCEF_CodigoContenedor",
                table: "CefContenedores",
                columns: new[] { "IdTransaccionCEF", "CodigoContenedor" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CEF_SOBRE_Padre",
                table: "CefContenedores",
                sql: "(([TipoContenedor] = 'Sobre' AND [IdContenedorPadre] IS NOT NULL) OR  ([TipoContenedor] <> 'Sobre' AND [IdContenedorPadre] IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CEF_SOBRE_TipoSobreValido",
                table: "CefContenedores",
                sql: "([TipoContenedor] <> 'Sobre') OR ([TipoSobre] IN ('Efectivo','Documento','Cheque'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CefContenedores_IdTransaccionCEF_CodigoContenedor",
                table: "CefContenedores");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CEF_SOBRE_Padre",
                table: "CefContenedores");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CEF_SOBRE_TipoSobreValido",
                table: "CefContenedores");

            migrationBuilder.DropColumn(
                name: "TipoSobre",
                table: "CefContenedores");

            migrationBuilder.CreateIndex(
                name: "IX_CefContenedores_IdTransaccionCEF",
                table: "CefContenedores",
                column: "IdTransaccionCEF");
        }
    }
}
