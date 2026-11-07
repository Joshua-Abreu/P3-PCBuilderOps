namespace PCBuilderOps.Services;

public interface IConversionCostoService
{
    Task<CostoConvertido> ConvertirAsync(ActorTaller actor, Guid ordenId, string? moneda, CancellationToken ct = default);
}

public record CostoConvertido(
    Guid OrdenId,
    string CodigoSeguimiento,
    string MonedaOrigen,
    decimal CostoOriginal,
    string MonedaDestino,
    decimal Tasa,
    decimal CostoEnMonedaDestino,
    DateTime TasaActualizadaUtc,
    string Fuente);
