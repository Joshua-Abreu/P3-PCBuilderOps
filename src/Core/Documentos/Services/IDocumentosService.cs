namespace Core.Documentos.Services;

public interface IDocumentosService
{
    Task<DocumentoDetalle> SubirAsync(Guid usuarioId, ArchivoSubido? archivo, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentoDetalle>> ListarAsync(ActorDocumento actor, CancellationToken ct = default);
    Task<DocumentoDetalle> ObtenerAsync(ActorDocumento actor, Guid documentoId, CancellationToken ct = default);
    Task<DescargaDocumento> DescargarAsync(ActorDocumento actor, Guid documentoId, CancellationToken ct = default);
    Task EliminarAsync(ActorDocumento actor, Guid documentoId, CancellationToken ct = default);
}

public record ActorDocumento(Guid UsuarioId, bool EsAdministrador);

public record ArchivoSubido(string? Nombre, string? TipoContenido, long Tamano, Stream Contenido);

public record DocumentoDetalle(
    Guid Id,
    string Nombre,
    string TipoContenido,
    long TamanoBytes,
    DateTime FechaSubidaUtc,
    Guid SubidoPorId,
    string SubidoPor);

public record DescargaDocumento(string Nombre, string TipoContenido, Stream Contenido);
