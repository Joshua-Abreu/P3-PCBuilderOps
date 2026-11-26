using System.Globalization;
using Core.Auth.Exceptions;

namespace Core.Reportes.Domain;

// RF-REP-05, RD-07, RD-11
public sealed record RangoFechas
{
    public const string Formato = "yyyy-MM-dd";

    private RangoFechas(DateOnly desde, DateOnly hasta)
    {
        Desde = desde;
        Hasta = hasta;
    }

    public DateOnly Desde { get; }
    public DateOnly Hasta { get; }

    public DateTime DesdeUtc => Desde.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    // Hallazgo 3 (Asignación 3)
    public DateTime HastaInclusivoUtc => Hasta.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

    public static RangoFechas Crear(string? desde, string? hasta)
    {
        if (string.IsNullOrWhiteSpace(desde) || string.IsNullOrWhiteSpace(hasta))
            throw new ValidacionException($"Indica desde y hasta con el formato {Formato}.");

        var inicio = Leer(desde, "desde");
        var fin = Leer(hasta, "hasta");

        if (inicio > fin)
            throw new ValidacionException("La fecha desde no puede ser posterior a hasta.");

        return new RangoFechas(inicio, fin);
    }

    private static DateOnly Leer(string valor, string nombre) =>
        DateOnly.TryParseExact(valor.Trim(), Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? fecha
            : throw new ValidacionException($"La fecha {nombre} no es válida. Usa el formato {Formato}.");
}
