using MailKit.Net.Smtp;
using MimeKit;

namespace Core.Notifications.Services;

public class SmtpEmailSender : ISmtpEmailSender
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _user;
    private readonly string _password;
    private readonly string _from;

    public SmtpEmailSender()
    {
        _host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? string.Empty;
        _port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
        _user = Environment.GetEnvironmentVariable("SMTP_USER") ?? string.Empty;
        _password = Environment.GetEnvironmentVariable("SMTP_PASS") ?? string.Empty;
        _from = Environment.GetEnvironmentVariable("SMTP_FROM") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_host))
            Console.WriteLine("[SMTP] ADVERTENCIA: SMTP_HOST no está configurado. El envío de correos no funcionará.");
    }

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_host))
            throw new InvalidOperationException("SMTP_HOST no está configurado.");

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress("PC Builder Ops", _from));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("plain") { Text = cuerpo };

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_host, _port, MailKit.Security.SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_user, _password, ct);
            await client.SendAsync(mensaje, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SMTP] Error enviando correo a {destinatario}: {ex.Message}");
            throw;
        }
    }
}
