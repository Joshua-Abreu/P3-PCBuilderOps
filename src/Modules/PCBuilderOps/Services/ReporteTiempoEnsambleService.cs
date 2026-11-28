using Core.Auth.Domain;
using Core.Reportes.Domain;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Domain;

namespace PCBuilderOps.Services;

// RF-REP-02, RF-REP-03, RF-REP-04, RF-REP-05, RF-NEG-08, CA-HIS-14 a CA-HIS-17
public class ReporteTiempoEnsambleService : IReporteTiempoEnsambleService
{
    private readonly PCBuilderOpsDbContext _dbContext;

    public ReporteTiempoEnsambleService(PCBuilderOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReporteTiempoEnsamble> TiempoEnsambleAsync(RangoFechas rango, Guid? soloDelTecnico, CancellationToken ct = default)
    {
        var armados =
            from transicion in _dbContext.TransicionesOrden.AsNoTracking()
            join orden in _dbContext.OrdenesEnsamble.AsNoTracking() on transicion.OrdenEnsambleId equals orden.Id
            where transicion.EstadoAnterior == EstadoOrdenEnsamble.EnArmado
                && transicion.EstadoNuevo == EstadoOrdenEnsamble.EnPruebas
                && transicion.SegundosEnEstadoAnterior != null
                && orden.TecnicoAsignadoId != null
                && transicion.FechaUtc >= rango.DesdeUtc
                && transicion.FechaUtc <= rango.HastaInclusivoUtc
            select new { TecnicoId = orden.TecnicoAsignadoId!.Value, Segundos = transicion.SegundosEnEstadoAnterior!.Value };

        if (soloDelTecnico is { } tecnicoId)
            armados = armados.Where(a => a.TecnicoId == tecnicoId);

        var grupos = await armados
            .GroupBy(a => a.TecnicoId)
            .Select(g => new
            {
                TecnicoId = g.Key,
                Ordenes = g.Count(),
                SegundosTotales = g.Sum(a => a.Segundos),
                Minimo = g.Min(a => a.Segundos),
                Maximo = g.Max(a => a.Segundos)
            })
            .ToListAsync(ct);

        var ids = grupos.Select(g => g.TecnicoId).ToList();
        var nombres = await _dbContext.Set<Usuario>()
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nombre, ct);

        var filas = grupos
            .Select(g => new FilaTecnico(
                g.TecnicoId,
                nombres.GetValueOrDefault(g.TecnicoId, string.Empty),
                g.Ordenes,
                Minutos((decimal)g.SegundosTotales / g.Ordenes),
                Minutos(g.Minimo),
                Minutos(g.Maximo)))
            .OrderByDescending(f => f.PromedioMinutos)
            .ThenBy(f => f.Tecnico)
            .ToList();

        var totalOrdenes = grupos.Sum(g => g.Ordenes);
        decimal? promedioGeneral = totalOrdenes == 0 ? null : Minutos((decimal)grupos.Sum(g => g.SegundosTotales) / totalOrdenes);

        return new ReporteTiempoEnsamble(rango.Desde, rango.Hasta, totalOrdenes, promedioGeneral, filas);
    }

    private static decimal Minutos(decimal segundos) => Math.Round(segundos / 60m, 1);
}
