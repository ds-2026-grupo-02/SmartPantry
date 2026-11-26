using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPantry.Migrations
{
    /// <inheritdoc />
    public partial class AgregadaTablaRegistrosLlamadasExternas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppRegistrosLlamadasExternas_FechaHoraUtc",
                table: "AppRegistrosLlamadasExternas");

            migrationBuilder.CreateIndex(
                name: "IX_AppRegistrosLlamadasExternas_FechaHoraUtc_TipoOperacion",
                table: "AppRegistrosLlamadasExternas",
                columns: new[] { "FechaHoraUtc", "TipoOperacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppRegistrosLlamadasExternas_FechaHoraUtc_TipoOperacion",
                table: "AppRegistrosLlamadasExternas");

            migrationBuilder.CreateIndex(
                name: "IX_AppRegistrosLlamadasExternas_FechaHoraUtc",
                table: "AppRegistrosLlamadasExternas",
                column: "FechaHoraUtc");
        }
    }
}
