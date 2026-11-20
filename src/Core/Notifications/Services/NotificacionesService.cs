using Core.Data;
using Core.Notifications.Domain;
using Core.Notifications.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Core.Notifications.Services;

public class NotificacionesService : INotificacionesService
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _reloj;

    public NotificacionesService(AppDbContext dbContext, TimeProvider reloj)
    {
        _dbContext = dbContext;
        _reloj = reloj;
    }

    private DateTime AhoraUtc => _reloj.GetUtcNow().UtcDateTime;

    // RF-NOT-04
    public async Task<BandejaNotificaciones> ListarAsync(Guid usuarioId, bool soloNoLeidas, CancellationToken ct = default)
    {
        var propias = _dbContext.Notificaciones
            .AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId);

        var noLeidas = await propias.CountAsync(n => n.FechaLecturaUtc == null, ct);

        if (soloNoLeidas)
            propias = propias.Where(n => n.FechaLecturaUtc == null);

        var notificaciones = await propias
            .OrderByDescending(n => n.FechaCreacionUtc)
            .ToListAsync(ct);

        return new BandejaNotificaciones(noLeidas, notificaciones.Select(ADetalle).ToList());
    }

    // RF-NOT-05
    public async Task<NotificacionDetalle> MarcarLeidaAsync(Guid usuarioId, Guid notificacionId, CancellationToken ct = default)
    {
        var notificacion = await _dbContext.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == notificacionId && n.UsuarioId == usuarioId, ct)
            ?? throw new NotificacionNoEncontradaException();

        if (!notificacion.EsLeida)
        {
            notificacion.MarcarLeida(AhoraUtc);
            await _dbContext.SaveChangesAsync(ct);
        }

        return ADetalle(notificacion);
    }

    // RF-NOT-07
    public async Task<PreferenciasNotificacion> ObtenerPreferenciasAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var correoActivo = await _dbContext.PreferenciasNotificacion
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => (bool?)p.CorreoActivo)
            .FirstOrDefaultAsync(ct);

        return new PreferenciasNotificacion(correoActivo ?? true);
    }

    // RF-NOT-07
    public async Task<PreferenciasNotificacion> ActualizarPreferenciasAsync(Guid usuarioId, bool correoActivo, CancellationToken ct = default)
    {
        var preferencia = await _dbContext.PreferenciasNotificacion
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

        if (preferencia is null)
            _dbContext.PreferenciasNotificacion.Add(PreferenciaNotificacion.Para(usuarioId, correoActivo, AhoraUtc));
        else
            preferencia.CambiarCorreo(correoActivo, AhoraUtc);

        await _dbContext.SaveChangesAsync(ct);

        return new PreferenciasNotificacion(correoActivo);
    }

    private static NotificacionDetalle ADetalle(Notificacion n) => new(
        n.Id,
        n.Asunto,
        n.Mensaje,
        n.EsLeida,
        ComoUtc(n.FechaCreacionUtc),
        n.FechaLecturaUtc is { } lectura ? ComoUtc(lectura) : null);

    // RD-11
    private static DateTime ComoUtc(DateTime fecha) => DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
}
