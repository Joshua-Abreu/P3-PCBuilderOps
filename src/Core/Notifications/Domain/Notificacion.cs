namespace Core.Notifications.Domain;

// RF-NOT-04, RF-NOT-05
public class Notificacion
{
    public const int LargoMaximoAsunto = 200;
    public const int LargoMaximoMensaje = 2000;

    private Notificacion() { }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Asunto { get; private set; } = string.Empty;
    public string Mensaje { get; private set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; private set; }
    public DateTime? FechaLecturaUtc { get; private set; }

    public bool EsLeida => FechaLecturaUtc is not null;

    public static Notificacion Nueva(Guid usuarioId, string asunto, string mensaje, DateTime ahoraUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(asunto);
        ArgumentException.ThrowIfNullOrWhiteSpace(mensaje);

        return new Notificacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Asunto = Recortar(asunto.Trim(), LargoMaximoAsunto),
            Mensaje = Recortar(mensaje.Trim(), LargoMaximoMensaje),
            FechaCreacionUtc = ahoraUtc
        };
    }

    // RF-NOT-05
    public void MarcarLeida(DateTime ahoraUtc) => FechaLecturaUtc ??= ahoraUtc;

    private static string Recortar(string texto, int largoMaximo) =>
        texto.Length <= largoMaximo ? texto : texto[..largoMaximo];
}
