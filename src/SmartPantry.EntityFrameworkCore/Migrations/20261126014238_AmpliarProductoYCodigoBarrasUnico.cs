using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPantry.Migrations
{
    /// <inheritdoc />
    public partial class AmpliarProductoYCodigoBarrasUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alergenos",
                table: "AppProductos",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categorias",
                table: "AppProductos",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "AppProductos",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ingredientes",
                table: "AppProductos",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "AppProductos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Nova",
                table: "AppProductos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutriScore",
                table: "AppProductos",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppProductos_CodigoBarras",
                table: "AppProductos",
                column: "CodigoBarras",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppProductos_CodigoBarras",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "Alergenos",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "Categorias",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "Ingredientes",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "Nova",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "NutriScore",
                table: "AppProductos");
        }
    }
}
