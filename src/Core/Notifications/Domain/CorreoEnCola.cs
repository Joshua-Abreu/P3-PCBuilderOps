namespace Core.Notifications.Domain;

// RF-NOT-11
public class CorreoEnCola
{
    public const int LargoMaximoError = 1000;

    public Guid Id { get; private set; }
    public string Destinatario { get; private set; } = string.Empty;
    public string Asunto { get; private set; } = string.Empty;
    public string Cuerpo { get; private set; } = string.Empty;
    public string Estado { get; private set; } = EstadoCorreo.Pendiente;
    public int Intentos { get; private set; }
    public string? UltimoError { get; private set; }
    public DateTime FechaCreacionUtc { get; private set; }
    public DateTime? FechaUltimoIntentoUtc { get; private set; }
    public DateTime? FechaEnvioUtc { get; private set; }
    public DateTime? ReservadoHastaUtc { get; private set; }

    private CorreoEnCola()
    {
    }

    // RF-NOT-08
    public static CorreoEnCola Nuevo(string destinatario, string asunto, string cuerpo, DateTime ahoraUtc) => new()
    {
        Id = Guid.NewGuid(),
        Destinatario = destinatario,
        Asunto = asunto,
        Cuerpo = cuerpo,
        Estado = EstadoCorreo.Pendiente,
        FechaCreacionUtc = ahoraUtc
    };

    // RF-NOT-12
    public void MarcarEnviado(DateTime ahoraUtc)
    {
        Intentos++;
        Estado = EstadoCorreo.Enviado;
        FechaUltimoIntentoUtc = ahoraUtc;
        FechaEnvioUtc = ahoraUtc;
        ReservadoHastaUtc = null;
    }

    // RF-NOT-10
    public void RegistrarFallo(string error, int maximoIntentos, DateTime ahoraUtc)
    {
        Intentos++;
        FechaUltimoIntentoUtc = ahoraUtc;
        UltimoError = error.Length > LargoMaximoError ? error[..LargoMaximoError] : error;
        ReservadoHastaUtc = null;

        if (Intentos >= maximoIntentos)
            Estado = EstadoCorreo.Fallido;
    }
}
