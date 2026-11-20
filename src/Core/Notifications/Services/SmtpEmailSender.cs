using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Core.Notifications.Services;

public class SmtpEmailSender : ISmtpEmailSender
{
    private readonly ConfiguracionSmtp _configuracion;

    public SmtpEmailSender(ConfiguracionSmtp configuracion)
    {
        _configuracion = configuracion;
    }

    private static SecureSocketOptions ObtenerOpcionSeguridad(int puerto) => puerto switch
    {
        465 => SecureSocketOptions.SslOnConnect,
        587 => SecureSocketOptions.StartTls,
        _ => SecureSocketOptions.Auto
    };

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_configuracion.Host))
            throw new InvalidOperationException("SMTP_HOST no está configurado.");

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress("PC Builder Ops", _configuracion.Remitente));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("plain") { Text = cuerpo };

        using var client = new SmtpClient();

        await client.ConnectAsync(_configuracion.Host, _configuracion.Puerto, ObtenerOpcionSeguridad(_configuracion.Puerto), ct);

        if (_configuracion.RequiereAutenticacion)
            await client.AuthenticateAsync(_configuracion.Usuario, _configuracion.Password, ct);

        await client.SendAsync(mensaje, ct);
        await client.DisconnectAsync(true, ct);
    }
}
