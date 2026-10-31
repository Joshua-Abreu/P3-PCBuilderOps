using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;

namespace PCBuilderOps.Services;

public class ConsultaOrdenesService : IConsultaOrdenesService
{
    private readonly PCBuilderOpsDbContext _dbContext;

    public ConsultaOrdenesService(PCBuilderOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OrdenResumen>> ListarAsync(CancellationToken ct = default)
    {
        var ordenes = await _dbContext.OrdenesEnsamble
            .AsNoTracking()
            .OrderByDescending(o => o.FechaCreacionUtc)
            .ToListAsync(ct);

        return ordenes
            .Select(o => new OrdenResumen(
                o.Id,
                o.CodigoSeguimiento,
                o.ClienteUsuarioId,
                o.TecnicoAsignadoId,
                o.Estado.ToString(),
                o.CostoTotal,
                o.FechaCreacionUtc))
            .ToList();
    }
}
