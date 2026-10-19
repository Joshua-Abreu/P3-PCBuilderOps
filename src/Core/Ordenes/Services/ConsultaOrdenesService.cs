using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Core.Ordenes.Services;

public class ConsultaOrdenesService : IConsultaOrdenesService
{
    private readonly AppDbContext _dbContext;

    public ConsultaOrdenesService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OrdenResumen>> ListarAsync(CancellationToken ct = default)
    {
        var ordenes = await _dbContext.OrdenesServicio
            .AsNoTracking()
            .OrderByDescending(o => o.FechaCreacionUtc)
            .ToListAsync(ct);

        return ordenes
            .Select(o => new OrdenResumen(o.Id, o.NumeroOrden, o.Cliente, o.Estado.ToString(), o.FechaCreacionUtc))
            .ToList();
    }
}
