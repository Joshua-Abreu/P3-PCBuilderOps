using Core.Data;
using Core.Notifications.Services;
using Core.Permisos.Domain;
using Core.Permisos.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Core.Permisos.Services;

public class GestionPermisosService : IGestionPermisosService, IVerificadorPermisos
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _reloj;
    private readonly INotificador _notificador;

    public GestionPermisosService(AppDbContext dbContext, TimeProvider reloj, INotificador notificador)
    {
        _dbContext = dbContext;
        _reloj = reloj;
        _notificador = notificador;
    }

    private DateTime AhoraUtc => _reloj.GetUtcNow().UtcDateTime;

    // RF-GP-01
    public async Task<SolicitudDetalle> CrearAsync(Guid solicitanteId, TipoPermiso permiso, CancellationToken ct = default)
    {
        if (await TienePermisoAsync(solicitanteId, permiso, ct))
            throw new SolicitudDuplicadaException("Ya tienes este permiso.");

        var tienePendiente = await _dbContext.SolicitudesPermiso
            .AnyAsync(s => s.SolicitanteId == solicitanteId && s.Permiso == permiso && s.Estado == EstadoSolicitud.Pendiente, ct);

        if (tienePendiente)
            throw new SolicitudDuplicadaException("Ya tienes una solicitud pendiente para este permiso.");

        var solicitud = SolicitudPermiso.Crear(solicitanteId, permiso, AhoraUtc);

        _dbContext.SolicitudesPermiso.Add(solicitud);
        await NotificarCreacionAsync(solicitud, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ADetalle(solicitud);
    }

    // RF-NOT-01
    private async Task NotificarCreacionAsync(SolicitudPermiso solicitud, CancellationToken ct)
    {
        var solicitante = await _dbContext.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == solicitud.SolicitanteId)
            .Select(u => new { u.Nombre, u.Correo })
            .SingleAsync(ct);

        await _notificador.NotificarAdministradoresAsync(
            "Nueva solicitud de permiso",
            $"{solicitante.Nombre} ({solicitante.Correo}) solicitó el permiso {solicitud.Permiso}. Revísala en las solicitudes pendientes.",
            excepto: solicitud.SolicitanteId,
            ct: ct);
    }

    // RF-GP-02
    public async Task<IReadOnlyList<SolicitudDetalle>> ListarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _dbContext.SolicitudesPermiso
            .AsNoTracking()
            .Where(s => s.Estado == EstadoSolicitud.Pendiente)
            .OrderBy(s => s.FechaCreacionUtc)
            .ToListAsync(ct);

        return pendientes.Select(ADetalle).ToList();
    }

    // RF-GP-02
    public async Task<IReadOnlyList<SolicitudDetalle>> ListarDelSolicitanteAsync(Guid solicitanteId, CancellationToken ct = default)
    {
        var propias = await _dbContext.SolicitudesPermiso
            .AsNoTracking()
            .Where(s => s.SolicitanteId == solicitanteId)
            .OrderByDescending(s => s.FechaCreacionUtc)
            .ToListAsync(ct);

        return propias.Select(ADetalle).ToList();
    }

    // RF-GP-03, RF-GP-05
    public async Task<SolicitudDetalle> AprobarAsync(Guid solicitudId, Guid aprobadorId, CancellationToken ct = default)
    {
        var solicitud = await ObtenerAsync(solicitudId, ct);
        var ahora = AhoraUtc;

        solicitud.Aprobar(aprobadorId, ahora);
        AplicarCambioDePermiso(solicitud, ahora);

        // RF-NOT-02
        await _notificador.NotificarAsync(
            solicitud.SolicitanteId,
            "Solicitud de permiso aprobada",
            $"Tu solicitud del permiso {solicitud.Permiso} fue aprobada. Ya tienes el permiso.",
            ct);

        await GuardarResolucionAsync(solicitud.Id, EstadoSolicitud.Aprobada, ct);

        return ADetalle(solicitud);
    }

    // RF-GP-05
    private void AplicarCambioDePermiso(SolicitudPermiso solicitud, DateTime ahoraUtc)
    {
        solicitud.Aplicar();
        _dbContext.PermisosUsuario.Add(PermisoUsuario.OtorgarPor(solicitud, ahoraUtc));
    }

    // RF-GP-04
    public async Task<SolicitudDetalle> RechazarAsync(Guid solicitudId, Guid aprobadorId, string? motivo, CancellationToken ct = default)
    {
        var solicitud = await ObtenerAsync(solicitudId, ct);

        solicitud.Rechazar(aprobadorId, motivo, AhoraUtc);

        // RF-NOT-02
        await _notificador.NotificarAsync(
            solicitud.SolicitanteId,
            "Solicitud de permiso rechazada",
            $"Tu solicitud del permiso {solicitud.Permiso} fue rechazada. Motivo: {solicitud.MotivoRechazo}",
            ct);

        await GuardarResolucionAsync(solicitud.Id, EstadoSolicitud.Rechazada, ct);

        return ADetalle(solicitud);
    }

    // Hallazgo 1 (Asignación 3), RF-GP-06
    private async Task GuardarResolucionAsync(Guid solicitudId, EstadoSolicitud hacia, CancellationToken ct)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var estadoGuardado = await _dbContext.SolicitudesPermiso
                .AsNoTracking()
                .Where(s => s.Id == solicitudId)
                .Select(s => (EstadoSolicitud?)s.Estado)
                .SingleOrDefaultAsync(ct);

            if (estadoGuardado is not { } estado || estado == EstadoSolicitud.Pendiente)
                throw;

            throw new TransicionNoPermitidaException(estado, hacia);
        }
    }

    // RF-GP-05
    public Task<bool> TienePermisoAsync(Guid usuarioId, TipoPermiso permiso, CancellationToken ct = default) =>
        _dbContext.PermisosUsuario.AnyAsync(p => p.UsuarioId == usuarioId && p.Permiso == permiso, ct);

    // RF-GP-05
    public async Task<IReadOnlyList<TipoPermiso>> ListarDelUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        await _dbContext.PermisosUsuario
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => p.Permiso)
            .OrderBy(p => p)
            .ToListAsync(ct);

    private async Task<SolicitudPermiso> ObtenerAsync(Guid solicitudId, CancellationToken ct) =>
        await _dbContext.SolicitudesPermiso.FirstOrDefaultAsync(s => s.Id == solicitudId, ct)
            ?? throw new SolicitudNoEncontradaException();

    private static SolicitudDetalle ADetalle(SolicitudPermiso s) => new(
        s.Id,
        s.SolicitanteId,
        s.Permiso.ToString(),
        s.Estado.ToString(),
        s.AprobadorId,
        s.FechaCreacionUtc,
        s.FechaResolucionUtc,
        s.MotivoRechazo);
}
