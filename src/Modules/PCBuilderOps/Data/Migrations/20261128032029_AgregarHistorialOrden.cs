using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCBuilderOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHistorialOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransicionesOrden",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenEnsambleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "int", nullable: true),
                    EstadoNuevo = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SegundosEnEstadoAnterior = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransicionesOrden", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransicionesOrden_OrdenesEnsamble_OrdenEnsambleId",
                        column: x => x.OrdenEnsambleId,
                        principalTable: "OrdenesEnsamble",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransicionesOrden_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransicionesOrden_EstadoNuevo_FechaUtc",
                table: "TransicionesOrden",
                columns: new[] { "EstadoNuevo", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TransicionesOrden_UsuarioId",
                table: "TransicionesOrden",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_TransicionesOrden_UnaPorEstado",
                table: "TransicionesOrden",
                columns: new[] { "OrdenEnsambleId", "EstadoNuevo" },
                unique: true);

            // CA-HIS-10
            migrationBuilder.Sql("""
                INSERT INTO TransicionesOrden (Id, OrdenEnsambleId, EstadoAnterior, EstadoNuevo, UsuarioId, FechaUtc, SegundosEnEstadoAnterior)
                SELECT NEWID(), Id, NULL, 1, ClienteUsuarioId, FechaCreacionUtc, NULL
                FROM OrdenesEnsamble;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransicionesOrden");
        }
    }
}
