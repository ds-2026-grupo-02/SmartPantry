using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPantry.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarTablasYColumnasAEspanol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Productos",
                table: "Productos");

            migrationBuilder.RenameTable(
                name: "Productos",
                newName: "AppProductos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppProductos",
                table: "AppProductos",
                column: "Id");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppPantryItems",
                table: "AppPantryItems");

            migrationBuilder.RenameTable(
                name: "AppPantryItems",
                newName: "AppItemsDespensa");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppItemsDespensa",
                table: "AppItemsDespensa",
                column: "Id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AppItemsDespensa",
                newName: "UsuarioId");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppExpirationWarnings",
                table: "AppExpirationWarnings");

            migrationBuilder.RenameTable(
                name: "AppExpirationWarnings",
                newName: "AppAdvertenciasVencimiento");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppAdvertenciasVencimiento",
                table: "AppAdvertenciasVencimiento",
                column: "Id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AppAdvertenciasVencimiento",
                newName: "UsuarioId");

            migrationBuilder.RenameColumn(
                name: "PantryItemId",
                table: "AppAdvertenciasVencimiento",
                newName: "ItemDespensaId");

            migrationBuilder.RenameColumn(
                name: "WarningType",
                table: "AppAdvertenciasVencimiento",
                newName: "TipoAdvertencia");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "AppAdvertenciasVencimiento",
                newName: "EsActiva");

            migrationBuilder.RenameColumn(
                name: "ExpirationDate",
                table: "AppAdvertenciasVencimiento",
                newName: "FechaVencimiento");

            migrationBuilder.RenameIndex(
                name: "IX_AppExpirationWarnings_PantryItemId_WarningType",
                table: "AppAdvertenciasVencimiento",
                newName: "IX_AppAdvertenciasVencimiento_ItemDespensaId_TipoAdvertencia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_AppAdvertenciasVencimiento_ItemDespensaId_TipoAdvertencia",
                table: "AppAdvertenciasVencimiento",
                newName: "IX_AppExpirationWarnings_PantryItemId_WarningType");

            migrationBuilder.RenameColumn(
                name: "FechaVencimiento",
                table: "AppAdvertenciasVencimiento",
                newName: "ExpirationDate");

            migrationBuilder.RenameColumn(
                name: "EsActiva",
                table: "AppAdvertenciasVencimiento",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "TipoAdvertencia",
                table: "AppAdvertenciasVencimiento",
                newName: "WarningType");

            migrationBuilder.RenameColumn(
                name: "ItemDespensaId",
                table: "AppAdvertenciasVencimiento",
                newName: "PantryItemId");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "AppAdvertenciasVencimiento",
                newName: "UserId");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppAdvertenciasVencimiento",
                table: "AppAdvertenciasVencimiento");

            migrationBuilder.RenameTable(
                name: "AppAdvertenciasVencimiento",
                newName: "AppExpirationWarnings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppExpirationWarnings",
                table: "AppExpirationWarnings",
                column: "Id");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "AppItemsDespensa",
                newName: "UserId");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppItemsDespensa",
                table: "AppItemsDespensa");

            migrationBuilder.RenameTable(
                name: "AppItemsDespensa",
                newName: "AppPantryItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppPantryItems",
                table: "AppPantryItems",
                column: "Id");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppProductos",
                table: "AppProductos");

            migrationBuilder.RenameTable(
                name: "AppProductos",
                newName: "Productos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Productos",
                table: "Productos",
                column: "Id");
        }
    }
}
