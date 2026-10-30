using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPantry.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProductoAndPantryItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "AppPantryItems");

            migrationBuilder.DropColumn(
                name: "Unidad",
                table: "AppPantryItems");

            migrationBuilder.AddColumn<string>(
                name: "Cantidad",
                table: "Productos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreEn",
                table: "Productos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreEs",
                table: "Productos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "NombreEn",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "NombreEs",
                table: "Productos");

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Productos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Cantidad",
                table: "AppPantryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unidad",
                table: "AppPantryItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }
    }
}
