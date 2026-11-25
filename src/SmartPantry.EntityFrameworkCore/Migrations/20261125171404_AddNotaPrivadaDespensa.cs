using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPantry.Migrations
{
    /// <inheritdoc />
    public partial class AddNotaPrivadaDespensa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppNotasPrivadasDespensa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemDespensaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotasPrivadasDespensa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppNotasPrivadasDespensa_AppItemsDespensa_ItemDespensaId",
                        column: x => x.ItemDespensaId,
                        principalTable: "AppItemsDespensa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppNotasPrivadasDespensa_ItemDespensaId",
                table: "AppNotasPrivadasDespensa",
                column: "ItemDespensaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppNotasPrivadasDespensa");
        }
    }
}
