using System.Text.Json;

namespace Core.Auditoria.Services;

// RF-AUD-06, RF-AUD-07
public interface IHistorialAuditoria
{
    Task<PaginaAuditoria> ConsultarAsync(ActorAuditoria actor, ConsultaAuditoria consulta, CancellationToken ct = default);
}

public record ActorAuditoria(Guid UsuarioId, bool EsAdministrador);

public record ConsultaAuditoria(
    string? UsuarioId,
    string? Entidad,
    string? Desde,
    string? Hasta,
    string? Pagina,
    string? TamanoPagina);

public record PaginaAuditoria(int Total, int Pagina, int TamanoPagina, IReadOnlyList<RegistroAuditoriaDetalle> Registros);

public record RegistroAuditoriaDetalle(
    Guid Id,
    Guid UsuarioId,
    string UsuarioNombre,
    string Accion,
    string Entidad,
    string EntidadId,
    DateTime FechaUtc,
    JsonElement ValorAnterior,
    JsonElement ValorNuevo);

public record EntidadesAuditables(IReadOnlyList<string> Nombres);
