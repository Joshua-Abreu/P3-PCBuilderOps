using PCBuilderOps.Domain;

namespace PCBuilderOps.Data;

// FN-01: catálogo de ejemplo para poder cotizar sin cargar datos a mano.
public static class ComponentesIniciales
{
    public static readonly Guid CpuAm5 = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    public static readonly Guid CpuLga1700 = Guid.Parse("a1000000-0000-0000-0000-000000000002");
    public static readonly Guid PlacaAm5 = Guid.Parse("a2000000-0000-0000-0000-000000000001");
    public static readonly Guid PlacaLga1700 = Guid.Parse("a2000000-0000-0000-0000-000000000002");
    public static readonly Guid GpuMedia = Guid.Parse("a3000000-0000-0000-0000-000000000001");
    public static readonly Guid GpuAlta = Guid.Parse("a3000000-0000-0000-0000-000000000002");
    public static readonly Guid Ram32Gb = Guid.Parse("a4000000-0000-0000-0000-000000000001");
    public static readonly Guid Ssd1Tb = Guid.Parse("a5000000-0000-0000-0000-000000000001");
    public static readonly Guid Fuente850W = Guid.Parse("a6000000-0000-0000-0000-000000000001");
    public static readonly Guid Fuente450W = Guid.Parse("a6000000-0000-0000-0000-000000000002");

    public static Componente[] Todos =>
    [
        Componente.Crear(CpuAm5, "Ryzen 7 7700X", CategoriaComponente.Cpu, "AMD", "AM5", 105, null, 299.00m, 10),
        Componente.Crear(CpuLga1700, "Core i5-13600K", CategoriaComponente.Cpu, "Intel", "LGA1700", 125, null, 279.00m, 10),
        Componente.Crear(PlacaAm5, "TUF Gaming B650-Plus", CategoriaComponente.PlacaBase, "ASUS", "AM5", 40, null, 189.00m, 10),
        Componente.Crear(PlacaLga1700, "PRO Z790-P", CategoriaComponente.PlacaBase, "MSI", "LGA1700", 40, null, 199.00m, 10),
        Componente.Crear(GpuMedia, "GeForce RTX 4060 Ventus", CategoriaComponente.Gpu, "MSI", "PCIe 4.0", 115, null, 299.00m, 10),
        Componente.Crear(GpuAlta, "GeForce RTX 4090 TUF", CategoriaComponente.Gpu, "ASUS", "PCIe 4.0", 450, null, 1799.00m, 1),
        Componente.Crear(Ram32Gb, "Vengeance 32 GB DDR5", CategoriaComponente.Ram, "Corsair", "DDR5", 10, null, 109.00m, 20),
        Componente.Crear(Ssd1Tb, "990 Pro 1 TB", CategoriaComponente.Almacenamiento, "Samsung", "NVMe", 8, null, 99.00m, 20),
        Componente.Crear(Fuente850W, "RM850x", CategoriaComponente.Fuente, "Corsair", "ATX", 0, 850, 139.00m, 10),
        Componente.Crear(Fuente450W, "450 BR", CategoriaComponente.Fuente, "EVGA", "ATX", 0, 450, 49.00m, 10)
    ];
}
