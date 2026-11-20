using Core.Reportes.Domain;

namespace Core.Reportes.Services;

public interface IReportesService
{
    Task<ReporteSolicitudes> SolicitudesPorEstadoAsync(RangoFechas rango, Guid? soloDelSolicitante, CancellationToken ct = default);
}

public record ReporteSolicitudes(DateOnly Desde, DateOnly Hasta, int Total, IReadOnlyList<FilaSolicitudes> Filas);

public record FilaSolicitudes(string Periodo, string Estado, int Cantidad);
