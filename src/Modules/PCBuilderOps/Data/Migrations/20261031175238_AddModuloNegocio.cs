using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PCBuilderOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Componentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    Fabricante = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SocketOInterfaz = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConsumoWatts = table.Column<int>(type: "int", nullable: false),
                    PotenciaWatts = table.Column<int>(type: "int", nullable: true),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockDisponible = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Componentes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesEnsamble",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoSeguimiento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClienteUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TecnicoAsignadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    CostoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesEnsamble", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesEnsamble_Usuarios_ClienteUsuarioId",
                        column: x => x.ClienteUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesEnsamble_Usuarios_TecnicoAsignadoId",
                        column: x => x.TecnicoAsignadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesEnsamble",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenEnsambleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesEnsamble", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesEnsamble_Componentes_ComponenteId",
                        column: x => x.ComponenteId,
                        principalTable: "Componentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesEnsamble_OrdenesEnsamble_OrdenEnsambleId",
                        column: x => x.OrdenEnsambleId,
                        principalTable: "OrdenesEnsamble",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PruebasRendimiento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenEnsambleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoSoftwarePrueba = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TemperaturaMaximaCpu = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TemperaturaMaximaGpu = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PuntajeBenchmark = table.Column<int>(type: "int", nullable: false),
                    EsAprobada = table.Column<bool>(type: "bit", nullable: false),
                    FechaEjecucionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PruebasRendimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PruebasRendimiento_OrdenesEnsamble_OrdenEnsambleId",
                        column: x => x.OrdenEnsambleId,
                        principalTable: "OrdenesEnsamble",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Componentes",
                columns: new[] { "Id", "Categoria", "ConsumoWatts", "Fabricante", "Nombre", "PotenciaWatts", "Precio", "SocketOInterfaz", "StockDisponible" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000001"), 1, 105, "AMD", "Ryzen 7 7700X", null, 299.00m, "AM5", 10 },
                    { new Guid("a1000000-0000-0000-0000-000000000002"), 1, 125, "Intel", "Core i5-13600K", null, 279.00m, "LGA1700", 10 },
                    { new Guid("a2000000-0000-0000-0000-000000000001"), 2, 40, "ASUS", "TUF Gaming B650-Plus", null, 189.00m, "AM5", 10 },
                    { new Guid("a2000000-0000-0000-0000-000000000002"), 2, 40, "MSI", "PRO Z790-P", null, 199.00m, "LGA1700", 10 },
                    { new Guid("a3000000-0000-0000-0000-000000000001"), 3, 115, "MSI", "GeForce RTX 4060 Ventus", null, 299.00m, "PCIe 4.0", 10 },
                    { new Guid("a3000000-0000-0000-0000-000000000002"), 3, 450, "ASUS", "GeForce RTX 4090 TUF", null, 1799.00m, "PCIe 4.0", 1 },
                    { new Guid("a4000000-0000-0000-0000-000000000001"), 4, 10, "Corsair", "Vengeance 32 GB DDR5", null, 109.00m, "DDR5", 20 },
                    { new Guid("a5000000-0000-0000-0000-000000000001"), 5, 8, "Samsung", "990 Pro 1 TB", null, 99.00m, "NVMe", 20 },
                    { new Guid("a6000000-0000-0000-0000-000000000001"), 6, 0, "Corsair", "RM850x", 850, 139.00m, "ATX", 10 },
                    { new Guid("a6000000-0000-0000-0000-000000000002"), 6, 0, "EVGA", "450 BR", 450, 49.00m, "ATX", 10 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesEnsamble_ComponenteId",
                table: "DetallesEnsamble",
                column: "ComponenteId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesEnsamble_OrdenEnsambleId",
                table: "DetallesEnsamble",
                column: "OrdenEnsambleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesEnsamble_ClienteUsuarioId",
                table: "OrdenesEnsamble",
                column: "ClienteUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesEnsamble_CodigoSeguimiento",
                table: "OrdenesEnsamble",
                column: "CodigoSeguimiento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesEnsamble_Estado",
                table: "OrdenesEnsamble",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesEnsamble_TecnicoAsignadoId",
                table: "OrdenesEnsamble",
                column: "TecnicoAsignadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PruebasRendimiento_OrdenEnsambleId",
                table: "PruebasRendimiento",
                column: "OrdenEnsambleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesEnsamble");

            migrationBuilder.DropTable(
                name: "PruebasRendimiento");

            migrationBuilder.DropTable(
                name: "Componentes");

            migrationBuilder.DropTable(
                name: "OrdenesEnsamble");
        }
    }
}
