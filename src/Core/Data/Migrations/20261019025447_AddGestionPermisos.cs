using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGestionPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesPermiso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permiso = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    AprobadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaResolucionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesPermiso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesPermiso_Usuarios_AprobadorId",
                        column: x => x.AprobadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesPermiso_Usuarios_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermisosUsuario",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permiso = table.Column<int>(type: "int", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaOtorgadoUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisosUsuario", x => new { x.UsuarioId, x.Permiso });
                    table.ForeignKey(
                        name: "FK_PermisosUsuario_SolicitudesPermiso_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "SolicitudesPermiso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermisosUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermisosUsuario_SolicitudId",
                table: "PermisosUsuario",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_AprobadorId",
                table: "SolicitudesPermiso",
                column: "AprobadorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_Estado",
                table: "SolicitudesPermiso",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_SolicitanteId",
                table: "SolicitudesPermiso",
                column: "SolicitanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermisosUsuario");

            migrationBuilder.DropTable(
                name: "SolicitudesPermiso");
        }
    }
}
