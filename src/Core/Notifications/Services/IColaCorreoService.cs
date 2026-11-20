namespace Core.Notifications.Services;

public interface IColaCorreoService
{
    // RF-NOT-09, RF-NOT-10, RF-NOT-12
    Task<ResultadoDespacho> DespacharPendientesAsync(CancellationToken ct = default);

    // RF-NOT-11
    Task<IReadOnlyList<CorreoResumen>> ListarAsync(string? estado, string? destinatario, CancellationToken ct = default);
}

public sealed record ResultadoDespacho(int Enviados, int ConError);

public sealed record CorreoResumen(
    Guid Id,
    string Destinatario,
    string Asunto,
    string Estado,
    int Intentos,
    DateTime FechaCreacionUtc,
    DateTime? FechaUltimoIntentoUtc,
    DateTime? FechaEnvioUtc,
    string? UltimoError);
