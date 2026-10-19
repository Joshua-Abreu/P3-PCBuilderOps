using Core.Permisos.Domain;

namespace Core.Permisos.Services;

public interface IGestionPermisosService
{
    Task<SolicitudDetalle> CrearAsync(Guid solicitanteId, TipoPermiso permiso, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudDetalle>> ListarPendientesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudDetalle>> ListarDelSolicitanteAsync(Guid solicitanteId, CancellationToken ct = default);
    Task<SolicitudDetalle> AprobarAsync(Guid solicitudId, Guid aprobadorId, CancellationToken ct = default);
    Task<SolicitudDetalle> RechazarAsync(Guid solicitudId, Guid aprobadorId, string? motivo, CancellationToken ct = default);
}

public record SolicitudDetalle(
    Guid Id,
    Guid SolicitanteId,
    string Permiso,
    string Estado,
    Guid? AprobadorId,
    DateTime FechaCreacionUtc,
    DateTime? FechaResolucionUtc,
    string? MotivoRechazo);
