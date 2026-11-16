using Core.Auth.Exceptions;

namespace PCBuilderOps.Domain;

// RF-NEG-01: datos tal como llegan del Administrador, sin validar.
public record DatosComponente(
    string? Nombre,
    string? Categoria,
    string? Fabricante,
    string? SocketOInterfaz,
    int? ConsumoWatts,
    int? PotenciaWatts,
    decimal? Precio,
    int? StockDisponible,
    string? TipoMemoria,
    int? RanurasMemoria);

internal sealed record DatosComponenteValidos(
    string Nombre,
    CategoriaComponente Categoria,
    string Fabricante,
    string SocketOInterfaz,
    int ConsumoWatts,
    int? PotenciaWatts,
    decimal Precio,
    int StockDisponible,
    string? TipoMemoria,
    int? RanurasMemoria);

// RD-07
internal static class ValidadorComponente
{
    public const int ConsumoMaximo = 2000;
    public const int PotenciaMaxima = 3000;
    public const int RanurasMaximas = 16;
    public const decimal PrecioMaximo = 100_000m;
    public const int StockMaximo = 100_000;

    public static DatosComponenteValidos Validar(DatosComponente datos)
    {
        var nombre = Texto(datos.Nombre, "El nombre", Componente.NombreMaximo);

        if (!EsCategoriaValida(datos.Categoria, out var categoria))
            throw new ValidacionException(
                $"La categoría no es válida. Usa {string.Join(", ", Enum.GetNames<CategoriaComponente>())}.");

        var fabricante = Texto(datos.Fabricante, "El fabricante", Componente.FabricanteMaximo);
        var socket = Texto(datos.SocketOInterfaz, "El socket o interfaz", Componente.SocketOInterfazMaximo);

        if (datos.ConsumoWatts is not { } consumo || consumo < 0 || consumo > ConsumoMaximo)
            throw new ValidacionException($"El consumo debe estar entre 0 y {ConsumoMaximo} W.");

        if (categoria == CategoriaComponente.Fuente)
        {
            if (datos.PotenciaWatts is not { } potencia || potencia < 1 || potencia > PotenciaMaxima)
                throw new ValidacionException($"La potencia de la fuente es obligatoria y debe estar entre 1 y {PotenciaMaxima} W.");
        }
        else if (datos.PotenciaWatts is not null)
        {
            throw new ValidacionException("Solo las fuentes de poder tienen potencia.");
        }

        var tipoMemoria = string.IsNullOrWhiteSpace(datos.TipoMemoria) ? null : datos.TipoMemoria.Trim().ToUpperInvariant();

        if (categoria != CategoriaComponente.PlacaBase && (tipoMemoria is not null || datos.RanurasMemoria is not null))
            throw new ValidacionException("Solo las placas base tienen tipo de memoria y ranuras.");

        if (tipoMemoria is { Length: > Componente.TipoMemoriaMaximo })
            throw new ValidacionException($"El tipo de memoria no puede superar {Componente.TipoMemoriaMaximo} caracteres.");

        if (datos.RanurasMemoria is { } ranuras && (ranuras < 1 || ranuras > RanurasMaximas))
            throw new ValidacionException($"Las ranuras de memoria deben estar entre 1 y {RanurasMaximas}.");

        if (datos.Precio is not { } precio || precio <= 0 || precio >= PrecioMaximo)
            throw new ValidacionException($"El precio debe ser mayor que 0 y menor que {PrecioMaximo:0}.");

        if (decimal.Round(precio, 2) != precio)
            throw new ValidacionException("El precio admite como máximo dos decimales.");

        if (datos.StockDisponible is not { } stock || stock < 0 || stock > StockMaximo)
            throw new ValidacionException($"Las existencias deben estar entre 0 y {StockMaximo}.");

        return new DatosComponenteValidos(
            nombre, categoria, fabricante, socket, consumo, datos.PotenciaWatts, precio, stock, tipoMemoria, datos.RanurasMemoria);
    }

    private static string Texto(string? valor, string campo, int maximo)
    {
        var texto = valor?.Trim() ?? string.Empty;

        if (texto.Length == 0)
            throw new ValidacionException($"{campo} es obligatorio.");

        if (texto.Length > maximo)
            throw new ValidacionException($"{campo} no puede superar {maximo} caracteres.");

        return texto;
    }

    private static bool EsCategoriaValida(string? valor, out CategoriaComponente categoria)
    {
        categoria = default;
        var texto = valor?.Trim() ?? string.Empty;

        return texto.Length > 0
            && !char.IsDigit(texto[0])
            && texto[0] != '-'
            && Enum.TryParse(texto, ignoreCase: true, out categoria)
            && Enum.IsDefined(categoria);
    }
}
