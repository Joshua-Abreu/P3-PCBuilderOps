using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public record GuardarComponenteRequest(
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

public record CambiarEstadoComponenteRequest([Required] bool? EstaActivo);
