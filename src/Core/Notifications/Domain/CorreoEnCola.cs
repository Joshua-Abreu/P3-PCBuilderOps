namespace Core.Notifications.Domain;

public class CorreoEnCola
{
    public Guid Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime? FechaEnvioUtc { get; set; }
}
