using System.Net;
using System.Text;
using Core.Auth.Exceptions;
using PCBuilderOps.TasaCambio;

namespace PCBuilderOps.Tests.TasaCambio;

// RD-07, RD-08, RD-10: la API externa se reemplaza por respuestas fijas; aquí se prueba cómo se interpretan.
public class ExchangeRateApiProveedorTests
{
    private const string Clave = "clave-secreta-de-prueba";
    private static readonly Uri UrlBase = new("https://tasas.prueba/v6/");

    private sealed class ManejadorFijo : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public ManejadorFijo(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        public HttpRequestMessage? Recibida { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Recibida = request;
            return Task.FromResult(_responder(request));
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode estado = HttpStatusCode.OK) =>
        new(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static (ExchangeRateApiProveedor Proveedor, ManejadorFijo Manejador) Crear(
        Func<HttpRequestMessage, HttpResponseMessage> responder, string? clave = Clave)
    {
        var manejador = new ManejadorFijo(responder);
        return (new ExchangeRateApiProveedor(new HttpClient(manejador), new ConfiguracionTasaCambio(clave, UrlBase)), manejador);
    }

    [Fact]
    public async Task Obtener_RespuestaExitosa_DevuelveLaTasaYSuFecha()
    {
        // Preparar
        var (proveedor, _) = Crear(_ => Json("""{"result":"success","base_code":"USD","target_code":"DOP","conversion_rate":60.4321,"time_last_update_unix":1762473601}"""));

        // Actuar
        var tasa = await proveedor.ObtenerAsync("USD", "DOP");

        // Verificar
        Assert.Equal(60.4321m, tasa.Valor);
        Assert.Equal("DOP", tasa.MonedaDestino);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1762473601).UtcDateTime, tasa.FechaActualizacionUtc);
    }

    [Fact]
    public async Task Obtener_EnviaLaClaveEnLaCabeceraYNoEnLaUrl()
    {
        // Preparar
        var (proveedor, manejador) = Crear(_ => Json("""{"result":"success","conversion_rate":1.5,"time_last_update_unix":0}"""));

        // Actuar
        await proveedor.ObtenerAsync("USD", "EUR");

        // Verificar
        Assert.Equal("Bearer", manejador.Recibida!.Headers.Authorization!.Scheme);
        Assert.Equal(Clave, manejador.Recibida.Headers.Authorization.Parameter);
        Assert.DoesNotContain(Clave, manejador.Recibida.RequestUri!.ToString());
        Assert.Equal("https://tasas.prueba/v6/pair/USD/EUR", manejador.Recibida.RequestUri.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Obtener_SinClave_SeRechazaSinLlamarALaApi(string? clave)
    {
        // Preparar
        var (proveedor, manejador) = Crear(_ => Json("{}"), clave);

        // Actuar
        var error = await Assert.ThrowsAsync<TasaCambioNoDisponibleException>(() => proveedor.ObtenerAsync("USD", "DOP"));

        // Verificar
        Assert.Contains(ConfiguracionTasaCambio.VariableClave, error.Message);
        Assert.Null(manejador.Recibida);
    }

    [Theory]
    [InlineData("invalid-key")]
    [InlineData("inactive-account")]
    [InlineData("quota-reached")]
    [InlineData("malformed-request")]
    public async Task Obtener_ErrorDeLaApi_SeRechazaSinMostrarLaClave(string tipoError)
    {
        // Preparar
        var (proveedor, _) = Crear(_ => Json($$"""{"result":"error","error-type":"{{tipoError}}"}""", HttpStatusCode.Forbidden));

        // Actuar
        var error = await Assert.ThrowsAsync<TasaCambioNoDisponibleException>(() => proveedor.ObtenerAsync("USD", "DOP"));

        // Verificar
        Assert.DoesNotContain(Clave, error.Message);
    }

    [Fact]
    public async Task Obtener_MonedaNoSoportada_SeRechazaComoDatoInvalido()
    {
        // Preparar
        var (proveedor, _) = Crear(_ => Json("""{"result":"error","error-type":"unsupported-code"}""", HttpStatusCode.NotFound));

        // Actuar
        var accion = () => proveedor.ObtenerAsync("USD", "XYZ");

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
    }

    [Fact]
    public async Task Obtener_ApiCaida_SeRechazaComoNoDisponible()
    {
        // Preparar
        var (proveedor, _) = Crear(_ => throw new HttpRequestException("No se pudo conectar"));

        // Actuar
        var accion = () => proveedor.ObtenerAsync("USD", "DOP");

        // Verificar
        await Assert.ThrowsAsync<TasaCambioNoDisponibleException>(accion);
    }

    [Fact]
    public async Task Obtener_ApiNoRespondeATiempo_SeRechazaComoNoDisponible()
    {
        // Preparar
        var (proveedor, _) = Crear(_ => throw new TaskCanceledException("Tiempo agotado"));

        // Actuar
        var accion = () => proveedor.ObtenerAsync("USD", "DOP");

        // Verificar
        await Assert.ThrowsAsync<TasaCambioNoDisponibleException>(accion);
    }

    [Fact]
    public async Task Obtener_RespuestaQueNoEsJson_SeRechazaComoNoDisponible()
    {
        // Preparar
        var (proveedor, _) = Crear(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>502</html>") });

        // Actuar
        var accion = () => proveedor.ObtenerAsync("USD", "DOP");

        // Verificar
        await Assert.ThrowsAsync<TasaCambioNoDisponibleException>(accion);
    }
}
