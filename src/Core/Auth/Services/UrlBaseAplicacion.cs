using Microsoft.Extensions.Configuration;

namespace Core.Auth.Services;

public static class UrlBaseAplicacion
{
    public const string VariableWeb = "WEB_BASE_URL";

    public static string Obtener(IConfiguration configuration) =>
        (Environment.GetEnvironmentVariable("APP_BASE_URL")
            ?? configuration["App:BaseUrl"]
            ?? "http://localhost:5206").TrimEnd('/');

    public static string? ObtenerWeb(IConfiguration configuration)
    {
        var url = Environment.GetEnvironmentVariable(VariableWeb) ?? configuration["App:WebBaseUrl"];
        return string.IsNullOrWhiteSpace(url) ? null : url.Trim().TrimEnd('/');
    }

    // RF-CA-16
    public static string EnlaceActivacion(IConfiguration configuration, string token) =>
        Enlace(configuration, "activar", "api/auth/activar", token);

    // RF-CA-10
    public static string EnlaceRecuperacion(IConfiguration configuration, string token) =>
        Enlace(configuration, "restablecer", "api/auth/restablecer-password", token);

    private static string Enlace(IConfiguration configuration, string rutaWeb, string rutaApi, string token)
    {
        var parametro = $"token={Uri.EscapeDataString(token)}";

        return ObtenerWeb(configuration) is { } web
            ? $"{web}/{rutaWeb}?{parametro}"
            : $"{Obtener(configuration)}/{rutaApi}?{parametro}";
    }
}
