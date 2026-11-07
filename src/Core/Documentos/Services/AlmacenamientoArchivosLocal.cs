namespace Core.Documentos.Services;

// RF-DOC-05, RD-12: la carpeta llega por constructor; en el contenedor es un volumen con nombre.
public class AlmacenamientoArchivosLocal : IAlmacenamientoArchivos
{
    private readonly string _carpeta;

    public AlmacenamientoArchivosLocal(string carpeta)
    {
        _carpeta = Path.GetFullPath(carpeta);
        Directory.CreateDirectory(_carpeta);
    }

    public async Task GuardarAsync(string nombreAlmacenado, Stream contenido, CancellationToken ct = default)
    {
        await using var destino = new FileStream(RutaDe(nombreAlmacenado), FileMode.CreateNew, FileAccess.Write);
        await contenido.CopyToAsync(destino, ct);
    }

    public Stream? AbrirLectura(string nombreAlmacenado)
    {
        var ruta = RutaDe(nombreAlmacenado);
        return File.Exists(ruta) ? new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
    }

    public void Borrar(string nombreAlmacenado)
    {
        var ruta = RutaDe(nombreAlmacenado);

        if (File.Exists(ruta))
            File.Delete(ruta);
    }

    private string RutaDe(string nombreAlmacenado)
    {
        var ruta = Path.GetFullPath(Path.Combine(_carpeta, nombreAlmacenado));

        if (!ruta.StartsWith(_carpeta + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Nombre de archivo fuera de la carpeta de documentos.");

        return ruta;
    }
}
