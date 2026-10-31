using Core.Auth.Exceptions;

namespace PCBuilderOps.Domain;

// FN-01
public static class VerificadorCompatibilidad
{
    public static void Validar(IReadOnlyCollection<(Componente Componente, int Cantidad)> componentes)
    {
        var cpu = ExigirUno(componentes, CategoriaComponente.Cpu, "una CPU");
        var placa = ExigirUno(componentes, CategoriaComponente.PlacaBase, "una placa base");
        var fuente = ExigirUno(componentes, CategoriaComponente.Fuente, "una fuente de poder");

        if (cpu.SocketOInterfaz != placa.SocketOInterfaz)
            throw new ValidacionException($"La CPU usa {cpu.SocketOInterfaz} y la placa base usa {placa.SocketOInterfaz}.");

        var consumoTotal = componentes.Sum(c => (long)c.Componente.ConsumoWatts * c.Cantidad);

        if (consumoTotal > fuente.PotenciaWatts)
            throw new ValidacionException($"La fuente entrega {fuente.PotenciaWatts} W y los componentes consumen {consumoTotal} W.");
    }

    private static Componente ExigirUno(
        IReadOnlyCollection<(Componente Componente, int Cantidad)> componentes,
        CategoriaComponente categoria,
        string nombre)
    {
        var deLaCategoria = componentes.Where(c => c.Componente.Categoria == categoria).ToList();

        if (deLaCategoria.Count != 1 || deLaCategoria[0].Cantidad != 1)
            throw new ValidacionException($"La cotización debe incluir exactamente {nombre}.");

        return deLaCategoria[0].Componente;
    }
}
