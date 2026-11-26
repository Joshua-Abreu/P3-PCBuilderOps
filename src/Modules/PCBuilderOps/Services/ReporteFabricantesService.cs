using Core.Reportes.Domain;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;

namespace PCBuilderOps.Services;

// RF-REP-02, RF-REP-03, RF-REP-04, RF-REP-05, RF-NEG-08
public class ReporteFabricantesService : IReporteFabricantesService
{
    private readonly PCBuilderOpsDbContext _dbContext;

    public ReporteFabricantesService(PCBuilderOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReporteFabricantes> FabricantesAsync(RangoFechas rango, Guid? soloDelCliente, CancellationToken ct = default)
    {
        var ordenes = _dbContext.OrdenesEnsamble
            .AsNoTracking()
            .Where(o => o.FechaCreacionUtc >= rango.DesdeUtc && o.FechaCreacionUtc <= rango.HastaInclusivoUtc);

        if (soloDelCliente is { } clienteId)
            ordenes = ordenes.Where(o => o.ClienteUsuarioId == clienteId);

        var grupos = await (
                from detalle in _dbContext.DetallesEnsamble.AsNoTracking()
                join orden in ordenes on detalle.OrdenEnsambleId equals orden.Id
                join componente in _dbContext.Componentes.AsNoTracking() on detalle.ComponenteId equals componente.Id
                group new { detalle.Cantidad, detalle.PrecioUnitario, detalle.OrdenEnsambleId } by componente.Fabricante into grupo
                select new
                {
                    Fabricante = grupo.Key,
                    Unidades = grupo.Sum(d => d.Cantidad),
                    ValorUsd = grupo.Sum(d => d.Cantidad * d.PrecioUnitario),
                    Ordenes = grupo.Select(d => d.OrdenEnsambleId).Distinct().Count()
                })
            .OrderByDescending(g => g.Unidades)
            .ThenByDescending(g => g.Ordenes)
            .ThenBy(g => g.Fabricante)
            .ToListAsync(ct);

        var filas = grupos
            .Select(g => new FilaFabricante(g.Fabricante, g.Unidades, g.ValorUsd, g.Ordenes))
            .ToList();

        return new ReporteFabricantes(
            rango.Desde,
            rango.Hasta,
            filas.Sum(f => f.Unidades),
            filas.Sum(f => f.ValorUsd),
            filas);
    }
}
