using Core.Data;
using Core.Permisos.Domain;
using Core.Permisos.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Core.Permisos.Services;

public class GestionPermisosService : IGestionPermisosService, IVerificadorPermisos
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _reloj;

    public GestionPermisosService(AppDbContext dbContext, TimeProvider reloj)
    {
        _dbContext = dbContext;
        _reloj = reloj;
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
        await _dbContext.SaveChangesAsync(ct);

        return ADetalle(solicitud);
    }

    // RF-GP-02
    public async Task<IReadOnlyList<SolicitudDetalle>> ListarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _dbContext.SolicitudesPermiso
            .AsNoTracking()
            .Where(s => s.Estado != EstadoSolicitud.Rechazada)
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

        await _dbContext.SaveChangesAsync(ct);

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

        await _dbContext.SaveChangesAsync(ct);

        return ADetalle(solicitud);
    }

    // RF-GP-05
    public Task<bool> TienePermisoAsync(Guid usuarioId, TipoPermiso permiso, CancellationToken ct = default) =>
        _dbContext.PermisosUsuario.AnyAsync(p => p.UsuarioId == usuarioId && p.Permiso == permiso, ct);

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
