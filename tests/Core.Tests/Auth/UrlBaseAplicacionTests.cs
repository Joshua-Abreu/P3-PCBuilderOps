using Core.Auth.Services;
using Microsoft.Extensions.Configuration;

namespace Core.Tests.Auth;

// RF-CA-10, RF-CA-16
public class UrlBaseAplicacionTests
{
    private const string Token = "0f8fad5bd9cb469fa16570867728950e";

    private static IConfiguration Configuracion(string? urlWeb) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "http://api.prueba:5206/",
                ["App:WebBaseUrl"] = urlWeb
            })
            .Build();

    [Fact]
    public void EnlaceActivacion_SinUrlDelFrontend_ApuntaALaApi()
    {
        // Preparar
        var configuracion = Configuracion(null);

        // Actuar
        var enlace = UrlBaseAplicacion.EnlaceActivacion(configuracion, Token);

        // Verificar
        Assert.Equal($"http://api.prueba:5206/api/auth/activar?token={Token}", enlace);
    }

    [Fact]
    public void EnlaceActivacion_ConUrlDelFrontend_ApuntaALaPantallaDeActivacion()
    {
        // Preparar
        var configuracion = Configuracion("http://web.prueba:5173/");

        // Actuar
        var enlace = UrlBaseAplicacion.EnlaceActivacion(configuracion, Token);

        // Verificar
        Assert.Equal($"http://web.prueba:5173/activar?token={Token}", enlace);
    }

    [Fact]
    public void EnlaceRecuperacion_SinUrlDelFrontend_ApuntaALaApi()
    {
        // Preparar
        var configuracion = Configuracion(null);

        // Actuar
        var enlace = UrlBaseAplicacion.EnlaceRecuperacion(configuracion, Token);

        // Verificar
        Assert.Equal($"http://api.prueba:5206/api/auth/restablecer-password?token={Token}", enlace);
    }

    [Fact]
    public void EnlaceRecuperacion_ConUrlDelFrontend_ApuntaALaPantallaDeRestablecimiento()
    {
        // Preparar
        var configuracion = Configuracion("http://web.prueba:5173");

        // Actuar
        var enlace = UrlBaseAplicacion.EnlaceRecuperacion(configuracion, Token);

        // Verificar
        Assert.Equal($"http://web.prueba:5173/restablecer?token={Token}", enlace);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnlaceActivacion_UrlDelFrontendVacia_ApuntaALaApi(string urlWeb)
    {
        // Preparar
        var configuracion = Configuracion(urlWeb);

        // Actuar
        var enlace = UrlBaseAplicacion.EnlaceActivacion(configuracion, Token);

        // Verificar
        Assert.StartsWith("http://api.prueba:5206/api/auth/activar?token=", enlace);
    }

    [Fact]
    public void EnlaceActivacion_TokenConCaracteresEspeciales_LoCodificaEnLaUrl()
    {
        // Preparar
        var configuracion = Configuracion("http://web.prueba:5173");

        // Actuar
        var enlace = UrlBaseAplicacion.EnlaceActivacion(configuracion, "a+b/c=&d");

        // Verificar
        Assert.Equal("http://web.prueba:5173/activar?token=a%2Bb%2Fc%3D%26d", enlace);
    }
}
