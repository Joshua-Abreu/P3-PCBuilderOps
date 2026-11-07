namespace PCBuilderOps.TasaCambio;

public interface IProveedorTasaCambio
{
    Task<TasaDeCambio> ObtenerAsync(string monedaOrigen, string monedaDestino, CancellationToken ct = default);
}

public record TasaDeCambio(string MonedaOrigen, string MonedaDestino, decimal Valor, DateTime FechaActualizacionUtc);
