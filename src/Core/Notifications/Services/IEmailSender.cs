namespace Core.Notifications.Services;

/// <summary>
/// Contrato para envío de correos electrónicos.
/// RF-NOT-13: Envío asíncrono vía SMTP sin bloquear operaciones.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Envía un correo electrónico de forma asíncrona.
    /// </summary>
    /// <param name="destinatario">Dirección de correo del destinatario.</param>
    /// <param name="asunto">Asunto del correo.</param>
    /// <param name="cuerpo">Cuerpo del correo.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default);
}
