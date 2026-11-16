using Core.Auth.Exceptions;
using Core.Documentos.Services;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Domain;
using PCBuilderOps.Exceptions;

namespace PCBuilderOps.Services;

public class CatalogoService : ICatalogoService
{
    public const long TamanoMaximoImagenBytes = 2 * 1024 * 1024;

    private readonly PCBuilderOpsDbContext _dbContext;
    private readonly IAlmacenamientoArchivos _almacenamiento;

    public CatalogoService(PCBuilderOpsDbContext dbContext, IAlmacenamientoArchivos almacenamiento)
    {
        _dbContext = dbContext;
        _almacenamiento = almacenamiento;
    }

    // FN-01, RF-NEG-06
    public async Task<IReadOnlyList<ComponenteResumen>> ListarAsync(bool incluirRetirados, CancellationToken ct = default)
    {
        var componentes = await _dbContext.Componentes
            .AsNoTracking()
            .Where(c => incluirRetirados || c.EstaActivo)
            .OrderBy(c => c.Categoria)
            .ThenBy(c => c.Nombre)
            .ToListAsync(ct);

        return componentes.Select(AResumen).ToList();
    }

    // RF-NEG-01, RD-07
    public async Task<ComponenteResumen> CrearAsync(DatosComponente datos, CancellationToken ct = default)
    {
        var componente = Componente.Registrar(Guid.NewGuid(), datos);

        _dbContext.Componentes.Add(componente);
        await _dbContext.SaveChangesAsync(ct);

        return AResumen(componente);
    }

    // RF-NEG-01, RD-07
    public async Task<ComponenteResumen> ActualizarAsync(Guid id, DatosComponente datos, CancellationToken ct = default)
    {
        var componente = await ObtenerAsync(id, ct);

        componente.Actualizar(datos);
        await _dbContext.SaveChangesAsync(ct);

        return AResumen(componente);
    }

    // RF-NEG-01
    public async Task<ComponenteResumen> CambiarEstadoAsync(Guid id, bool estaActivo, CancellationToken ct = default)
    {
        var componente = await ObtenerAsync(id, ct);

        componente.CambiarEstado(estaActivo);
        await _dbContext.SaveChangesAsync(ct);

        return AResumen(componente);
    }

    // RF-DOC-01, RF-DOC-05, RD-07
    public async Task<ComponenteResumen> CambiarImagenAsync(Guid id, ImagenSubida? imagen, CancellationToken ct = default)
    {
        var componente = await ObtenerAsync(id, ct);

        if (imagen is null)
            throw new ValidacionException("Debes adjuntar una imagen en el campo «archivo».");

        if (imagen.Tamano <= 0)
            throw new ValidacionException("La imagen está vacía.");

        if (imagen.Tamano > TamanoMaximoImagenBytes)
            throw new ValidacionException(MensajeTamano);

        await using var copia = new MemoryStream();
        await CopiarConLimiteAsync(imagen.Contenido, copia, ct);

        var tipoContenido = DetectorImagen.TipoContenido(copia.GetBuffer().AsSpan(0, (int)copia.Length))
            ?? throw new ValidacionException("La imagen debe ser JPG, PNG o WebP.");

        var archivo = $"componente-{Guid.NewGuid():N}";
        copia.Position = 0;
        await _almacenamiento.GuardarAsync(archivo, copia, ct);

        var anterior = componente.CambiarImagen(archivo, tipoContenido);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            _almacenamiento.Borrar(archivo);
            throw;
        }

        if (anterior is not null)
            _almacenamiento.Borrar(anterior);

        return AResumen(componente);
    }

    // RF-DOC-04
    public async Task<ComponenteResumen> QuitarImagenAsync(Guid id, CancellationToken ct = default)
    {
        var componente = await ObtenerAsync(id, ct);
        var anterior = componente.QuitarImagen();

        await _dbContext.SaveChangesAsync(ct);

        if (anterior is not null)
            _almacenamiento.Borrar(anterior);

        return AResumen(componente);
    }

    // FN-01, RF-NEG-06
    public async Task<ImagenComponente> ObtenerImagenAsync(Guid id, CancellationToken ct = default)
    {
        var componente = await _dbContext.Componentes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new ComponenteNoEncontradoException();

        if (componente.ImagenArchivo is null || componente.ImagenTipoContenido is null)
            throw new ImagenNoEncontradaException();

        var contenido = _almacenamiento.AbrirLectura(componente.ImagenArchivo)
            ?? throw new ImagenNoEncontradaException();

        return new ImagenComponente(componente.ImagenTipoContenido, contenido);
    }

    public static ComponenteResumen AResumen(Componente c) => new(
        c.Id,
        c.Nombre,
        c.Categoria.ToString(),
        c.Fabricante,
        c.SocketOInterfaz,
        c.ConsumoWatts,
        c.PotenciaWatts,
        c.Precio,
        c.StockDisponible,
        c.TipoMemoria,
        c.RanurasMemoria,
        c.EstaActivo,
        c.ImagenArchivo is null ? null : $"/api/componentes/{c.Id}/imagen?v={c.ImagenArchivo}");

    private static string MensajeTamano => $"La imagen supera el máximo de {TamanoMaximoImagenBytes / (1024 * 1024)} MB.";

    private static async Task CopiarConLimiteAsync(Stream origen, MemoryStream destino, CancellationToken ct)
    {
        var bufer = new byte[81920];
        int leidos;

        while ((leidos = await origen.ReadAsync(bufer, ct)) > 0)
        {
            if (destino.Length + leidos > TamanoMaximoImagenBytes)
                throw new ValidacionException(MensajeTamano);

            await destino.WriteAsync(bufer.AsMemory(0, leidos), ct);
        }
    }

    private async Task<Componente> ObtenerAsync(Guid id, CancellationToken ct) =>
        await _dbContext.Componentes.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new ComponenteNoEncontradoException();
}
