using Microsoft.Extensions.Configuration;

namespace Core.Auth.Services;

public static class UrlBaseAplicacion
{
    public static string Obtener(IConfiguration configuration) =>
        (Environment.GetEnvironmentVariable("APP_BASE_URL")
            ?? configuration["App:BaseUrl"]
            ?? "http://localhost:5206").TrimEnd('/');
}
