using Core.Data;
using Core.Reportes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Reportes.Services;

// RF-REP-01, RF-REP-03, RF-REP-04, RF-REP-05
public class ReportesService : IReportesService
{
    private readonly AppDbContext _dbContext;

    public ReportesService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReporteSolicitudes> SolicitudesPorEstadoAsync(RangoFechas rango, Guid? soloDelSolicitante, CancellationToken ct = default)
    {
        var solicitudes = _dbContext.SolicitudesPermiso
            .AsNoTracking()
            .Where(s => s.FechaCreacionUtc >= rango.DesdeUtc && s.FechaCreacionUtc < rango.HastaExclusivoUtc);

        if (soloDelSolicitante is { } solicitanteId)
            solicitudes = solicitudes.Where(s => s.SolicitanteId == solicitanteId);

        var grupos = await solicitudes
            .GroupBy(s => new { s.FechaCreacionUtc.Year, s.FechaCreacionUtc.Month, s.Estado })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Estado, Cantidad = g.Count() })
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Month)
            .ThenBy(g => g.Estado)
            .ToListAsync(ct);

        var filas = grupos
            .Select(g => new FilaSolicitudes($"{g.Year:D4}-{g.Month:D2}", g.Estado.ToString(), g.Cantidad))
            .ToList();

        return new ReporteSolicitudes(rango.Desde, rango.Hasta, filas.Sum(f => f.Cantidad), filas);
    }
}
