using Core.Documentos.Exceptions;

namespace Core.Documentos.Domain;

// RF-DOC-01, RF-DOC-04
public class Documento
{
    public const int NombreMaximo = 255;
    public const int TipoContenidoMaximo = 100;

    private Documento() { }

    public Guid Id { get; private set; }
    public string NombreOriginal { get; private set; } = string.Empty;
    public string TipoContenido { get; private set; } = string.Empty;
    public long TamanoBytes { get; private set; }
    public DateTime FechaSubidaUtc { get; private set; }
    public Guid SubidoPorId { get; private set; }
    public string NombreAlmacenado { get; private set; } = string.Empty;
    public bool EstaEliminado { get; private set; }
    public DateTime? FechaEliminacionUtc { get; private set; }
    public Guid? EliminadoPorId { get; private set; }

    internal static Documento Crear(
        Guid id,
        string nombreOriginal,
        string tipoContenido,
        long tamanoBytes,
        Guid subidoPorId,
        string nombreAlmacenado,
        DateTime ahoraUtc) => new()
    {
        Id = id,
        NombreOriginal = nombreOriginal,
        TipoContenido = tipoContenido,
        TamanoBytes = tamanoBytes,
        FechaSubidaUtc = ahoraUtc,
        SubidoPorId = subidoPorId,
        NombreAlmacenado = nombreAlmacenado
    };

    // RF-DOC-03, RD-06
    public void ValidarAcceso(Guid usuarioId, bool esAdministrador)
    {
        if (!esAdministrador && usuarioId != SubidoPorId)
            throw new AccesoDocumentoDenegadoException();
    }

    // RF-DOC-04
    public void Eliminar(Guid usuarioId, DateTime ahoraUtc)
    {
        EstaEliminado = true;
        FechaEliminacionUtc = ahoraUtc;
        EliminadoPorId = usuarioId;
    }
}
