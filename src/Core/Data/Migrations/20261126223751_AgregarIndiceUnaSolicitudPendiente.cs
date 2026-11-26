using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIndiceUnaSolicitudPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_SolicitudesPermiso_UnaPendientePorPermiso",
                table: "SolicitudesPermiso",
                columns: new[] { "SolicitanteId", "Permiso" },
                unique: true,
                filter: "[Estado] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SolicitudesPermiso_UnaPendientePorPermiso",
                table: "SolicitudesPermiso");
        }
    }
}
