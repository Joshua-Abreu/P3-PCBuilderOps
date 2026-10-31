namespace PCBuilderOps.Services;

public interface IConsultaOrdenesService
{
    Task<IReadOnlyList<OrdenResumen>> ListarAsync(CancellationToken ct = default);
}

public record OrdenResumen(
    Guid Id,
    string CodigoSeguimiento,
    Guid ClienteUsuarioId,
    Guid? TecnicoAsignadoId,
    string Estado,
    decimal CostoTotal,
    DateTime FechaCreacionUtc);
