namespace Core.Notifications.Services;

public interface ISmtpEmailSender
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default);
}
