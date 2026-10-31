using Core.Auth.Exceptions;

namespace PCBuilderOps.Domain;

// RF-NEG-01, FN-03
public class PruebaRendimiento
{
    public const int TipoSoftwarePruebaMaximo = 100;

    private PruebaRendimiento() { }

    public Guid Id { get; private set; }
    public Guid OrdenEnsambleId { get; private set; }
    public string TipoSoftwarePrueba { get; private set; } = string.Empty;
    public decimal TemperaturaMaximaCpu { get; private set; }
    public decimal TemperaturaMaximaGpu { get; private set; }
    public int PuntajeBenchmark { get; private set; }
    public bool EsAprobada { get; private set; }
    public DateTime FechaEjecucionUtc { get; private set; }

    internal static PruebaRendimiento Crear(
        Guid ordenEnsambleId,
        string? tipoSoftwarePrueba,
        decimal temperaturaMaximaCpu,
        decimal temperaturaMaximaGpu,
        int puntajeBenchmark,
        bool esAprobada,
        DateTime ahoraUtc)
    {
        if (string.IsNullOrWhiteSpace(tipoSoftwarePrueba))
            throw new ValidacionException("El software de la prueba es obligatorio.");

        var tipoLimpio = tipoSoftwarePrueba.Trim();

        if (tipoLimpio.Length > TipoSoftwarePruebaMaximo)
            throw new ValidacionException($"El software de la prueba no puede superar {TipoSoftwarePruebaMaximo} caracteres.");

        if (temperaturaMaximaCpu <= 0 || temperaturaMaximaGpu <= 0)
            throw new ValidacionException("Las temperaturas máximas deben ser mayores que cero.");

        if (puntajeBenchmark < 0)
            throw new ValidacionException("El puntaje del benchmark no puede ser negativo.");

        return new PruebaRendimiento
        {
            Id = Guid.NewGuid(),
            OrdenEnsambleId = ordenEnsambleId,
            TipoSoftwarePrueba = tipoLimpio,
            TemperaturaMaximaCpu = temperaturaMaximaCpu,
            TemperaturaMaximaGpu = temperaturaMaximaGpu,
            PuntajeBenchmark = puntajeBenchmark,
            EsAprobada = esAprobada,
            FechaEjecucionUtc = ahoraUtc
        };
    }
}
