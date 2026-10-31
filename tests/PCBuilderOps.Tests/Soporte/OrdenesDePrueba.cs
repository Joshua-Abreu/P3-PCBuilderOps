using PCBuilderOps.Domain;

namespace PCBuilderOps.Tests.Soporte;

public static class OrdenesDePrueba
{
    public static readonly Guid ClienteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TecnicoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly DateTime Fecha = new(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc);

    public static Componente Cpu(string socket = "AM5", int consumoWatts = 100, int stock = 5) =>
        Componente.Crear(Guid.NewGuid(), "CPU", CategoriaComponente.Cpu, "AMD", socket, consumoWatts, null, 300m, stock);

    public static Componente Placa(string socket = "AM5", int consumoWatts = 50) =>
        Componente.Crear(Guid.NewGuid(), "Placa", CategoriaComponente.PlacaBase, "ASUS", socket, consumoWatts, null, 200m, 5);

    public static Componente Fuente(int potenciaWatts = 500) =>
        Componente.Crear(Guid.NewGuid(), "Fuente", CategoriaComponente.Fuente, "Corsair", "ATX", 0, potenciaWatts, 100m, 5);

    public static Componente Gpu(int consumoWatts = 200) =>
        Componente.Crear(Guid.NewGuid(), "GPU", CategoriaComponente.Gpu, "MSI", "PCIe 4.0", consumoWatts, null, 400m, 5);

    public static OrdenEnsamble Cotizada(int stockCpu = 5) =>
        OrdenEnsamble.Cotizar(ClienteId, [(Cpu(stock: stockCpu), 1), (Placa(), 1), (Fuente(), 1)], Fecha);

    public static OrdenEnsamble EnEstado(EstadoOrdenEnsamble estado)
    {
        var orden = Cotizada();

        if (estado == EstadoOrdenEnsamble.Cotizada)
            return orden;

        orden.IniciarArmado(TecnicoId);

        if (estado == EstadoOrdenEnsamble.EnArmado)
            return orden;

        orden.PasarAPruebas();

        if (estado == EstadoOrdenEnsamble.EnPruebas)
            return orden;

        RegistrarPrueba(orden, esAprobada: true, Fecha);
        orden.Entregar();

        return orden;
    }

    public static PruebaRendimiento RegistrarPrueba(OrdenEnsamble orden, bool esAprobada, DateTime fecha) =>
        orden.RegistrarPrueba("Cinebench", 70m, 65m, 12000, esAprobada, fecha);
}
