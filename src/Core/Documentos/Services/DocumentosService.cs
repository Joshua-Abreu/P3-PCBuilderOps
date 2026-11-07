using System.Net.Http.Headers;
using Core.Auth.Exceptions;
using Core.Data;
using Core.Documentos.Domain;
using Core.Documentos.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Core.Documentos.Services;

public class DocumentosService : IDocumentosService
{
    public const long TamanoMaximoBytes = 10 * 1024 * 1024;
    private const string TipoContenidoGenerico = "application/octet-stream";

    private readonly AppDbContext _dbContext;
    private readonly IAlmacenamientoArchivos _almacenamiento;
    private readonly TimeProvider _reloj;

    public DocumentosService(AppDbContext dbContext, IAlmacenamientoArchivos almacenamiento, TimeProvider reloj)
    {
        _dbContext = dbContext;
        _almacenamiento = almacenamiento;
        _reloj = reloj;
    }

    private DateTime AhoraUtc => _reloj.GetUtcNow().UtcDateTime;

    // RF-DOC-01, RD-07
    public async Task<DocumentoDetalle> SubirAsync(Guid usuarioId, ArchivoSubido? archivo, CancellationToken ct = default)
    {
        if (archivo is null)
            throw new ValidacionException("Debes adjuntar un archivo en el campo «archivo».");

        if (archivo.Tamano <= 0)
            throw new ValidacionException("El archivo está vacío.");

        if (archivo.Tamano > TamanoMaximoBytes)
            throw new ValidacionException($"El archivo supera el máximo de {TamanoMaximoBytes / (1024 * 1024)} MB.");

        var nombre = Path.GetFileName((archivo.Nombre ?? string.Empty).Replace('\\', '/')).Trim();

        if (nombre.Length == 0)
            throw new ValidacionException("El archivo debe tener un nombre.");

        if (nombre.Length > Documento.NombreMaximo)
            throw new ValidacionException($"El nombre del archivo no puede superar {Documento.NombreMaximo} caracteres.");

        var tipoContenido = MediaTypeHeaderValue.TryParse(archivo.TipoContenido, out var tipo)
            && tipo.MediaType is { Length: > 0 and <= Documento.TipoContenidoMaximo } tipoValido
                ? tipoValido
                : TipoContenidoGenerico;

        var id = Guid.NewGuid();
        var nombreAlmacenado = id.ToString("N");

        await _almacenamiento.GuardarAsync(nombreAlmacenado, archivo.Contenido, ct);

        var documento = Documento.Crear(id, nombre, tipoContenido, archivo.Tamano, usuarioId, nombreAlmacenado, AhoraUtc);

        try
        {
            _dbContext.Documentos.Add(documento);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            _almacenamiento.Borrar(nombreAlmacenado);
            throw;
        }

        return await ObtenerDetalleAsync(documento.Id, ct);
    }

    // RF-DOC-02
    public async Task<IReadOnlyList<DocumentoDetalle>> ListarAsync(ActorDocumento actor, CancellationToken ct = default)
    {
        var visibles = _dbContext.Documentos
            .AsNoTracking()
            .Where(d => !d.EstaEliminado);

        if (!actor.EsAdministrador)
            visibles = visibles.Where(d => d.SubidoPorId == actor.UsuarioId);

        return await ADetalle(visibles.OrderByDescending(d => d.FechaSubidaUtc))
            .ToListAsync(ct);
    }

    // RF-DOC-01, RF-DOC-03
    public async Task<DocumentoDetalle> ObtenerAsync(ActorDocumento actor, Guid documentoId, CancellationToken ct = default)
    {
        var documento = await ObtenerVigenteAsync(documentoId, ct);

        documento.ValidarAcceso(actor.UsuarioId, actor.EsAdministrador);

        return await ObtenerDetalleAsync(documento.Id, ct);
    }

    // RF-DOC-03
    public async Task<DescargaDocumento> DescargarAsync(ActorDocumento actor, Guid documentoId, CancellationToken ct = default)
    {
        var documento = await ObtenerVigenteAsync(documentoId, ct);

        documento.ValidarAcceso(actor.UsuarioId, actor.EsAdministrador);

        var contenido = _almacenamiento.AbrirLectura(documento.NombreAlmacenado)
            ?? throw new DocumentoNoEncontradoException();

        return new DescargaDocumento(documento.NombreOriginal, documento.TipoContenido, contenido);
    }

    // RF-DOC-04
    public async Task EliminarAsync(ActorDocumento actor, Guid documentoId, CancellationToken ct = default)
    {
        var documento = await ObtenerVigenteAsync(documentoId, ct);

        documento.ValidarAcceso(actor.UsuarioId, actor.EsAdministrador);
        documento.Eliminar(actor.UsuarioId, AhoraUtc);

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<Documento> ObtenerVigenteAsync(Guid documentoId, CancellationToken ct) =>
        await _dbContext.Documentos.FirstOrDefaultAsync(d => d.Id == documentoId && !d.EstaEliminado, ct)
            ?? throw new DocumentoNoEncontradoException();

    private Task<DocumentoDetalle> ObtenerDetalleAsync(Guid documentoId, CancellationToken ct) =>
        ADetalle(_dbContext.Documentos.AsNoTracking().Where(d => d.Id == documentoId)).SingleAsync(ct);

    private IQueryable<DocumentoDetalle> ADetalle(IQueryable<Documento> documentos) =>
        from d in documentos
        join u in _dbContext.Usuarios on d.SubidoPorId equals u.Id
        select new DocumentoDetalle(d.Id, d.NombreOriginal, d.TipoContenido, d.TamanoBytes, d.FechaSubidaUtc, d.SubidoPorId, u.Nombre);
}
