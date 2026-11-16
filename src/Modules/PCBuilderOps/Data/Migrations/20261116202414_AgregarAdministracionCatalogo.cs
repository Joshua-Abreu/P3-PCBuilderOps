using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCBuilderOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAdministracionCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EstaActivo",
                table: "Componentes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenArchivo",
                table: "Componentes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenTipoContenido",
                table: "Componentes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-0000-0000-000000000001"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-0000-0000-000000000002"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-0000-0000-000000000003"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-0000-0000-000000000001"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-0000-0000-000000000002"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-0000-0000-000000000001"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-0000-0000-000000000002"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a5000000-0000-0000-0000-000000000001"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a6000000-0000-0000-0000-000000000001"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a6000000-0000-0000-0000-000000000002"),
                columns: new[] { "EstaActivo", "ImagenArchivo", "ImagenTipoContenido" },
                values: new object[] { true, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstaActivo",
                table: "Componentes");

            migrationBuilder.DropColumn(
                name: "ImagenArchivo",
                table: "Componentes");

            migrationBuilder.DropColumn(
                name: "ImagenTipoContenido",
                table: "Componentes");
        }
    }
}
