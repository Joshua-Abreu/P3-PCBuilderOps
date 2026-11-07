using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TipoContenido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    FechaSubidaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubidoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreAlmacenado = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaEliminacionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Usuarios_EliminadoPorId",
                        column: x => x.EliminadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documentos_Usuarios_SubidoPorId",
                        column: x => x.SubidoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_EliminadoPorId",
                table: "Documentos",
                column: "EliminadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_NombreAlmacenado",
                table: "Documentos",
                column: "NombreAlmacenado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_SubidoPorId_EstaEliminado",
                table: "Documentos",
                columns: new[] { "SubidoPorId", "EstaEliminado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documentos");
        }
    }
}
