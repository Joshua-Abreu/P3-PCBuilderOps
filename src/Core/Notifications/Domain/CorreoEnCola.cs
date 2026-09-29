namespace Core.Notifications.Domain;

/// <summary>
/// Correo en cola para envío asíncrono vía SMTP.
/// RF-NOT-08: Cola de correos con estados Pendiente y Enviado.
/// RD-11: Todas las fechas en UTC.
/// </summary>
public class CorreoEnCola
{
    public Guid Id { get; set; }

    /// <summary>Dirección de correo del destinatario.</summary>
    public string Destinatario { get; set; } = string.Empty;

    /// <summary>Asunto del correo.</summary>
    public string Asunto { get; set; } = string.Empty;

    /// <summary>Cuerpo del correo.</summary>
    public string Cuerpo { get; set; } = string.Empty;

    /// <summary>Estado del envío: Pendiente o Enviado.</summary>
    public string Estado { get; set; } = "Pendiente";

    /// <summary>Fecha de creación del registro en la cola en UTC (RD-11).</summary>
    public DateTime FechaCreacionUtc { get; set; }

    /// <summary>Fecha de envío del correo en UTC. Null mientras no se envía.</summary>
    public DateTime? FechaEnvioUtc { get; set; }
}
