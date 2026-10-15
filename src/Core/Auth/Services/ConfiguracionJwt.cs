using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Core.Auth.Services;

// RD-10
public sealed record ConfiguracionJwt(string Secreto, string Emisor, string Audiencia, int ExpiracionMinutos)
{
    public const int LongitudMinimaSecretoBytes = 32;

    public SymmetricSecurityKey ClaveFirma => new(Encoding.UTF8.GetBytes(Secreto));

    public TokenValidationParameters ParametrosValidacion() => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = ClaveFirma,
        ValidateIssuer = true,
        ValidIssuer = Emisor,
        ValidateAudience = true,
        ValidAudience = Audiencia,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    public static ConfiguracionJwt DesdeEntorno()
    {
        var secreto = Environment.GetEnvironmentVariable("JWT_SECRET");

        if (string.IsNullOrWhiteSpace(secreto))
            throw new InvalidOperationException(
                "Falta la variable de entorno JWT_SECRET. Defínela antes de ejecutar la aplicación (ver README).");

        if (Encoding.UTF8.GetByteCount(secreto) < LongitudMinimaSecretoBytes)
            throw new InvalidOperationException(
                $"JWT_SECRET debe tener al menos {LongitudMinimaSecretoBytes} bytes para firmar con HMAC-SHA256.");

        var expiracionMinutos = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRACION_MINUTOS"), out var minutos) && minutos > 0
            ? minutos
            : 60;

        return new ConfiguracionJwt(
            secreto,
            Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "PCBuilderOps",
            Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "PCBuilderOps",
            expiracionMinutos);
    }
}
