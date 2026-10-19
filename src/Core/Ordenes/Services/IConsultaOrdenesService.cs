namespace Core.Ordenes.Services;

public interface IConsultaOrdenesService
{
    Task<IReadOnlyList<OrdenResumen>> ListarAsync(CancellationToken ct = default);
}

public record OrdenResumen(Guid Id, string NumeroOrden, string Cliente, string Estado, DateTime FechaCreacionUtc);
