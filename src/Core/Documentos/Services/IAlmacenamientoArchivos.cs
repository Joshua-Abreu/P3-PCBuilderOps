namespace Core.Documentos.Services;

// RF-DOC-05
public interface IAlmacenamientoArchivos
{
    Task GuardarAsync(string nombreAlmacenado, Stream contenido, CancellationToken ct = default);
    Stream? AbrirLectura(string nombreAlmacenado);
    void Borrar(string nombreAlmacenado);
}
