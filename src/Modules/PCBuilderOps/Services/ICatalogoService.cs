using PCBuilderOps.Domain;

namespace PCBuilderOps.Services;

public interface ICatalogoService
{
    Task<IReadOnlyList<ComponenteResumen>> ListarAsync(bool incluirRetirados, CancellationToken ct = default);
    Task<ComponenteResumen> CrearAsync(DatosComponente datos, CancellationToken ct = default);
    Task<ComponenteResumen> ActualizarAsync(Guid id, DatosComponente datos, CancellationToken ct = default);
    Task<ComponenteResumen> CambiarEstadoAsync(Guid id, bool estaActivo, CancellationToken ct = default);
    Task<ComponenteResumen> CambiarImagenAsync(Guid id, ImagenSubida? imagen, CancellationToken ct = default);
    Task<ComponenteResumen> QuitarImagenAsync(Guid id, CancellationToken ct = default);
    Task<ImagenComponente> ObtenerImagenAsync(Guid id, CancellationToken ct = default);
}

public record ComponenteResumen(
    Guid Id,
    string Nombre,
    string Categoria,
    string Fabricante,
    string SocketOInterfaz,
    int ConsumoWatts,
    int? PotenciaWatts,
    decimal Precio,
    int StockDisponible,
    string? TipoMemoria,
    int? RanurasMemoria,
    bool EstaActivo,
    string? ImagenUrl);

public record ImagenSubida(long Tamano, Stream Contenido);

public record ImagenComponente(string TipoContenido, Stream Contenido);
