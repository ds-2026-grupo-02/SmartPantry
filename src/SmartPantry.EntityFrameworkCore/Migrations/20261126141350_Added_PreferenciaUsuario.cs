using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPantry.Migrations
{
    /// <inheritdoc />
    public partial class Added_PreferenciaUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppPreferenciasUsuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MostrarAlertas = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPreferenciasUsuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPreferenciasAlergenos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PreferenciaUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPreferenciasAlergenos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPreferenciasAlergenos_AppPreferenciasUsuario_PreferenciaUsuarioId",
                        column: x => x.PreferenciaUsuarioId,
                        principalTable: "AppPreferenciasUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppPreferenciasIngredientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PreferenciaUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPreferenciasIngredientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPreferenciasIngredientes_AppPreferenciasUsuario_PreferenciaUsuarioId",
                        column: x => x.PreferenciaUsuarioId,
                        principalTable: "AppPreferenciasUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppPreferenciasAlergenos_PreferenciaUsuarioId_Nombre",
                table: "AppPreferenciasAlergenos",
                columns: new[] { "PreferenciaUsuarioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPreferenciasIngredientes_PreferenciaUsuarioId_Nombre",
                table: "AppPreferenciasIngredientes",
                columns: new[] { "PreferenciaUsuarioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPreferenciasUsuario_UsuarioId",
                table: "AppPreferenciasUsuario",
                column: "UsuarioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPreferenciasAlergenos");

            migrationBuilder.DropTable(
                name: "AppPreferenciasIngredientes");

            migrationBuilder.DropTable(
                name: "AppPreferenciasUsuario");
        }
    }
}
