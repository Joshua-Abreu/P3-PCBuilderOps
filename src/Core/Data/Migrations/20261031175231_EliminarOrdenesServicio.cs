using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class EliminarOrdenesServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdenesServicio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdenesServicio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cliente = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumeroOrden = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesServicio", x => x.Id);
                });
        }
    }
}
