using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReintentosColaCorreos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimoIntentoUtc",
                table: "CorreosEnCola",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Intentos",
                table: "CorreosEnCola",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservadoHastaUtc",
                table: "CorreosEnCola",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoError",
                table: "CorreosEnCola",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE CorreosEnCola SET Intentos = 1, FechaUltimoIntentoUtc = FechaEnvioUtc WHERE Estado = 'Enviado';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaUltimoIntentoUtc",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "Intentos",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "ReservadoHastaUtc",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "UltimoError",
                table: "CorreosEnCola");
        }
    }
}
