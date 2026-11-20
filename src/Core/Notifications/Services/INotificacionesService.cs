namespace Core.Notifications.Services;

public interface INotificacionesService
{
    Task<BandejaNotificaciones> ListarAsync(Guid usuarioId, bool soloNoLeidas, CancellationToken ct = default);
    Task<NotificacionDetalle> MarcarLeidaAsync(Guid usuarioId, Guid notificacionId, CancellationToken ct = default);
    Task<PreferenciasNotificacion> ObtenerPreferenciasAsync(Guid usuarioId, CancellationToken ct = default);
    Task<PreferenciasNotificacion> ActualizarPreferenciasAsync(Guid usuarioId, bool correoActivo, CancellationToken ct = default);
}

public record BandejaNotificaciones(int NoLeidas, IReadOnlyList<NotificacionDetalle> Notificaciones);

public record NotificacionDetalle(
    Guid Id,
    string Asunto,
    string Mensaje,
    bool EsLeida,
    DateTime FechaCreacionUtc,
    DateTime? FechaLecturaUtc);

public record PreferenciasNotificacion(bool CorreoActivo);
