using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PCBuilderOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCompatibilidadMemoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RanurasMemoria",
                table: "Componentes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoMemoria",
                table: "Componentes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-0000-0000-000000000001"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { 4, "DDR5" });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-0000-0000-000000000002"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { 4, "DDR5" });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-0000-0000-000000000001"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-0000-0000-000000000002"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-0000-0000-000000000001"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a5000000-0000-0000-0000-000000000001"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a6000000-0000-0000-0000-000000000001"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a6000000-0000-0000-0000-000000000002"),
                columns: new[] { "RanurasMemoria", "TipoMemoria" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "Componentes",
                columns: new[] { "Id", "Categoria", "ConsumoWatts", "Fabricante", "Nombre", "PotenciaWatts", "Precio", "RanurasMemoria", "SocketOInterfaz", "StockDisponible", "TipoMemoria" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000003"), 1, 65, "AMD", "Ryzen 5 5600", null, 129.00m, null, "AM4", 10, null },
                    { new Guid("a2000000-0000-0000-0000-000000000003"), 2, 35, "Gigabyte", "B550M DS3H", null, 99.00m, 4, "AM4", 10, "DDR4" },
                    { new Guid("a4000000-0000-0000-0000-000000000002"), 4, 8, "Kingston", "Fury Beast 16 GB DDR4", null, 45.00m, null, "DDR4", 20, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Componentes",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-0000-0000-000000000002"));

            migrationBuilder.DropColumn(
                name: "RanurasMemoria",
                table: "Componentes");

            migrationBuilder.DropColumn(
                name: "TipoMemoria",
                table: "Componentes");
        }
    }
}
