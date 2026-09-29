namespace Core.Notifications.Services;

public interface IEmailSender
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default);
}
