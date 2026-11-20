namespace Core.Notifications.Services;

// RF-NOT-13, RD-10, RD-12: las credenciales solo llegan del entorno, sin valores de respaldo.
public sealed record ConfiguracionSmtp(string Host, int Puerto, string Usuario, string Password, string Remitente)
{
    public bool RequiereAutenticacion => !string.IsNullOrWhiteSpace(Usuario);

    public static ConfiguracionSmtp DesdeEntorno() => new(
        Environment.GetEnvironmentVariable("SMTP_HOST") ?? string.Empty,
        int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var puerto) ? puerto : 587,
        Environment.GetEnvironmentVariable("SMTP_USER") ?? string.Empty,
        Environment.GetEnvironmentVariable("SMTP_PASS") ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? string.Empty,
        Environment.GetEnvironmentVariable("SMTP_FROM") ?? string.Empty);
}
