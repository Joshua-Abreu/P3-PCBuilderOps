using Core.Auth.Exceptions;

namespace PCBuilderOps.Domain;

public sealed record ResultadoCompatibilidad(
    IReadOnlyList<CategoriaComponente> Faltantes,
    IReadOnlyList<string> Problemas,
    long ConsumoWatts,
    int? PotenciaWatts)
{
    public bool EsCompatible => Faltantes.Count == 0 && Problemas.Count == 0;

    internal string? PrimerError { get; init; }
}

// FN-01
public static class VerificadorCompatibilidad
{
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
        var errores = new List<string>();
        var unicos = new Dictionary<CategoriaComponente, Componente>();

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
                problemas.Add(mensaje);
                errores.Add(mensaje);
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
            var mensaje = $"La CPU usa {cpu.SocketOInterfaz} y la placa base usa {placa.SocketOInterfaz}.";
            problemas.Add(mensaje);
            errores.Add(mensaje);
        }

        var consumoTotal = componentes.Sum(c => (long)c.Componente.ConsumoWatts * c.Cantidad);
        var potencia = unicos.TryGetValue(CategoriaComponente.Fuente, out var fuente) ? fuente.PotenciaWatts : null;

        if (potencia is { } watts && consumoTotal > watts)
        {
            var mensaje = $"La fuente entrega {watts} W y los componentes consumen {consumoTotal} W.";
            problemas.Add(mensaje);
            errores.Add(mensaje);
        }

        return new ResultadoCompatibilidad(faltantes, problemas, consumoTotal, potencia)
        {
            PrimerError = errores.FirstOrDefault()
        };
    }
}
