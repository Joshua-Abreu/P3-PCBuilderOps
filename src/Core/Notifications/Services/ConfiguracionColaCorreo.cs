namespace Core.Notifications.Services;

// RF-NOT-10, RD-12
public sealed record ConfiguracionColaCorreo(int MaximoIntentos, TimeSpan DuracionReserva)
{
    public const int MaximoIntentosPorDefecto = 3;
    public static readonly TimeSpan ReservaPorDefecto = TimeSpan.FromMinutes(5);

    public static ConfiguracionColaCorreo DesdeEntorno()
    {
        var valor = Environment.GetEnvironmentVariable("CORREO_MAX_INTENTOS");

        if (string.IsNullOrWhiteSpace(valor))
            return new ConfiguracionColaCorreo(MaximoIntentosPorDefecto, ReservaPorDefecto);

        if (!int.TryParse(valor, out var maximo) || maximo < 1)
            throw new InvalidOperationException("CORREO_MAX_INTENTOS debe ser un número entero mayor que cero.");

        return new ConfiguracionColaCorreo(maximo, ReservaPorDefecto);
    }
}
