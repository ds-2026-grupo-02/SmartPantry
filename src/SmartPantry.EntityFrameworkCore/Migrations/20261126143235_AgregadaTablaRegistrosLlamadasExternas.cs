using System;
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
            migrationBuilder.CreateTable(
                name: "AppRegistrosLlamadasExternas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoOperacion = table.Column<int>(type: "int", nullable: false),
                    FechaHoraUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DuracionMs = table.Column<long>(type: "bigint", nullable: false),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    CodigoHttp = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRegistrosLlamadasExternas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppRegistrosLlamadasExternas_FechaHoraUtc",
                table: "AppRegistrosLlamadasExternas",
                column: "FechaHoraUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AppRegistrosLlamadasExternas_FechaHoraUtc_Resultado",
                table: "AppRegistrosLlamadasExternas",
                columns: new[] { "FechaHoraUtc", "Resultado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppRegistrosLlamadasExternas");
        }
    }
}
