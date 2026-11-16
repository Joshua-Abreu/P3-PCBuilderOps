using Core.Auth.Exceptions;

namespace PCBuilderOps.Domain;

public sealed record ResultadoCompatibilidad(
    IReadOnlyList<CategoriaComponente> Faltantes,
    IReadOnlyList<string> Problemas,
    IReadOnlyList<string> Advertencias,
    long ConsumoWatts,
    int? PotenciaWatts)
{
    public bool EsCompatible => Faltantes.Count == 0 && Problemas.Count == 0;

    internal string? PrimerError { get; init; }
}

// FN-01
public static class VerificadorCompatibilidad
{
    public const int MargenFuentePorcentaje = 20;

    private static readonly (CategoriaComponente Categoria, string Nombre)[] Obligatorios =
    [
        (CategoriaComponente.Cpu, "una CPU"),
        (CategoriaComponente.PlacaBase, "una placa base"),
        (CategoriaComponente.Fuente, "una fuente de poder")
    ];

    public static void Validar(IReadOnlyCollection<(Componente Componente, int Cantidad)> componentes)
    {
        if (Evaluar(componentes).PrimerError is { } error)
            throw new ValidacionException(error);
    }

    public static ResultadoCompatibilidad Evaluar(IReadOnlyCollection<(Componente Componente, int Cantidad)> componentes)
    {
        var faltantes = new List<CategoriaComponente>();
        var problemas = new List<string>();
        var advertencias = new List<string>();
        var errores = new List<string>();
        var unicos = new Dictionary<CategoriaComponente, Componente>();

        void AgregarProblema(string mensaje)
        {
            problemas.Add(mensaje);
            errores.Add(mensaje);
        }

        foreach (var (categoria, nombre) in Obligatorios)
        {
            var deLaCategoria = componentes.Where(c => c.Componente.Categoria == categoria).ToList();
            var mensaje = $"La cotización debe incluir exactamente {nombre}.";

            if (deLaCategoria.Count == 0)
            {
                faltantes.Add(categoria);
                errores.Add(mensaje);
            }
            else if (deLaCategoria.Count != 1 || deLaCategoria[0].Cantidad != 1)
            {
                AgregarProblema(mensaje);
            }
            else
            {
                unicos[categoria] = deLaCategoria[0].Componente;
            }
        }

        if (unicos.TryGetValue(CategoriaComponente.Cpu, out var cpu)
            && unicos.TryGetValue(CategoriaComponente.PlacaBase, out var placa)
            && cpu.SocketOInterfaz != placa.SocketOInterfaz)
        {
            AgregarProblema($"La CPU usa {cpu.SocketOInterfaz} y la placa base usa {placa.SocketOInterfaz}.");
        }

        if (unicos.TryGetValue(CategoriaComponente.PlacaBase, out var placaBase))
        {
            foreach (var mensaje in ProblemasDeMemoria(placaBase, componentes))
                AgregarProblema(mensaje);
        }

        var consumoTotal = componentes.Sum(c => (long)c.Componente.ConsumoWatts * c.Cantidad);
        var potencia = unicos.TryGetValue(CategoriaComponente.Fuente, out var fuente) ? fuente.PotenciaWatts : null;

        if (potencia is { } watts)
        {
            if (consumoTotal > watts)
                AgregarProblema($"La fuente entrega {watts} W y los componentes consumen {consumoTotal} W.");
            else if (consumoTotal * 100 > (long)watts * (100 - MargenFuentePorcentaje))
                advertencias.Add(
                    $"La fuente queda con menos del {MargenFuentePorcentaje} % de margen: entrega {watts} W y los componentes consumen {consumoTotal} W.");
        }

        return new ResultadoCompatibilidad(faltantes, problemas, advertencias, consumoTotal, potencia)
        {
            PrimerError = errores.FirstOrDefault()
        };
    }

    private static IEnumerable<string> ProblemasDeMemoria(
        Componente placa,
        IReadOnlyCollection<(Componente Componente, int Cantidad)> componentes)
    {
        var memorias = componentes.Where(c => c.Componente.Categoria == CategoriaComponente.Ram).ToList();

        if (placa.TipoMemoria is { } tipo)
        {
            foreach (var (memoria, _) in memorias.Where(m => m.Componente.SocketOInterfaz != tipo))
                yield return $"La memoria {memoria.Nombre} es {memoria.SocketOInterfaz} y la placa base usa {tipo}.";
        }

        var modulos = memorias.Sum(m => m.Cantidad);

        if (placa.RanurasMemoria is { } ranuras && modulos > ranuras)
            yield return $"La placa base tiene {ranuras} ranuras de memoria y el armado lleva {modulos} módulos.";
    }
}
