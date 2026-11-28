using Core.Reportes.Domain;

namespace PCBuilderOps.Services;

public interface IReporteTiempoEnsambleService
{
    Task<ReporteTiempoEnsamble> TiempoEnsambleAsync(RangoFechas rango, Guid? soloDelTecnico, CancellationToken ct = default);
}

public record ReporteTiempoEnsamble(
    DateOnly Desde,
    DateOnly Hasta,
    int TotalOrdenes,
    decimal? PromedioGeneralMinutos,
    IReadOnlyList<FilaTecnico> Filas);

public record FilaTecnico(
    Guid TecnicoId,
    string Tecnico,
    int Ordenes,
    decimal PromedioMinutos,
    decimal MinimoMinutos,
    decimal MaximoMinutos);
