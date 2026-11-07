namespace PCBuilderOps.TasaCambio;

// RD-10: la clave solo llega por variable de entorno; RD-12: el proveedor la recibe por constructor.
public record ConfiguracionTasaCambio(string? ClaveApi, Uri UrlBase)
{
    public const string VariableClave = "TASA_CAMBIO_API_KEY";
    public const string VariableUrlBase = "TASA_CAMBIO_URL_BASE";
    public static readonly Uri UrlBasePorDefecto = new("https://v6.exchangerate-api.com/v6/");

    public bool TieneClave => !string.IsNullOrWhiteSpace(ClaveApi);

    public static ConfiguracionTasaCambio DesdeEntorno()
    {
        var urlBase = Environment.GetEnvironmentVariable(VariableUrlBase);

        return new ConfiguracionTasaCambio(
            Environment.GetEnvironmentVariable(VariableClave)?.Trim(),
            string.IsNullOrWhiteSpace(urlBase) ? UrlBasePorDefecto : new Uri(urlBase.TrimEnd('/') + "/"));
    }
}
