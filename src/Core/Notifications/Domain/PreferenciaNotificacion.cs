namespace Core.Notifications.Domain;

// RF-NOT-07
public class PreferenciaNotificacion
{
    private PreferenciaNotificacion() { }

    public Guid UsuarioId { get; private set; }
    public bool CorreoActivo { get; private set; }
    public DateTime FechaActualizacionUtc { get; private set; }

    public static PreferenciaNotificacion Para(Guid usuarioId, bool correoActivo, DateTime ahoraUtc) => new()
    {
        UsuarioId = usuarioId,
        CorreoActivo = correoActivo,
        FechaActualizacionUtc = ahoraUtc
    };

    public void CambiarCorreo(bool correoActivo, DateTime ahoraUtc)
    {
        CorreoActivo = correoActivo;
        FechaActualizacionUtc = ahoraUtc;
    }
}
