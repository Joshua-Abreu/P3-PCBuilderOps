using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Auth.Exceptions;

namespace PCBuilderOps.TasaCambio;

// RD-07, RD-08, RD-10: la clave viaja en la cabecera, nunca en la URL ni en los mensajes.
public class ExchangeRateApiProveedor : IProveedorTasaCambio
{
    private const string Servicio = "El servicio de tasa de cambio";

    private readonly HttpClient _http;
    private readonly ConfiguracionTasaCambio _configuracion;

    public ExchangeRateApiProveedor(HttpClient http, ConfiguracionTasaCambio configuracion)
    {
        _http = http;
        _configuracion = configuracion;
    }

    public async Task<TasaDeCambio> ObtenerAsync(string monedaOrigen, string monedaDestino, CancellationToken ct = default)
    {
        if (!_configuracion.TieneClave)
            throw new TasaCambioNoDisponibleException(
                $"La conversión de moneda no está configurada: define {ConfiguracionTasaCambio.VariableClave} en el archivo .env.");

        using var peticion = new HttpRequestMessage(HttpMethod.Get, new Uri(_configuracion.UrlBase, $"pair/{monedaOrigen}/{monedaDestino}"));
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuracion.ClaveApi);

        RespuestaExchangeRate? respuesta;

        try
        {
            using var http = await _http.SendAsync(peticion, ct);
            await using var cuerpo = await http.Content.ReadAsStreamAsync(ct);
            respuesta = await JsonSerializer.DeserializeAsync<RespuestaExchangeRate>(cuerpo, cancellationToken: ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            throw new TasaCambioNoDisponibleException($"{Servicio} no responde. Intenta más tarde.");
        }

        if (respuesta is { Resultado: "success", Tasa: > 0 })
            return new TasaDeCambio(
                monedaOrigen,
                monedaDestino,
                respuesta.Tasa.Value,
                DateTimeOffset.FromUnixTimeSeconds(respuesta.ActualizadaUnix).UtcDateTime);

        throw respuesta?.TipoError switch
        {
            "unsupported-code" => new ValidacionException($"La moneda {monedaDestino} no está soportada por {Servicio.ToLowerInvariant()}."),
            "invalid-key" or "inactive-account" => new TasaCambioNoDisponibleException(
                $"{Servicio} rechazó la clave configurada en {ConfiguracionTasaCambio.VariableClave}."),
            "quota-reached" => new TasaCambioNoDisponibleException($"{Servicio} agotó la cuota de consultas. Intenta más tarde."),
            _ => new TasaCambioNoDisponibleException($"{Servicio} no devolvió una tasa válida. Intenta más tarde.")
        };
    }

    private sealed record RespuestaExchangeRate(
        [property: JsonPropertyName("result")] string? Resultado,
        [property: JsonPropertyName("error-type")] string? TipoError,
        [property: JsonPropertyName("conversion_rate")] decimal? Tasa,
        [property: JsonPropertyName("time_last_update_unix")] long ActualizadaUnix);
}
