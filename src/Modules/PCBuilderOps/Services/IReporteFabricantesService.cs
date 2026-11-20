using Core.Reportes.Domain;

namespace PCBuilderOps.Services;

public interface IReporteFabricantesService
{
    Task<ReporteFabricantes> FabricantesAsync(RangoFechas rango, Guid? soloDelCliente, CancellationToken ct = default);
}

public record ReporteFabricantes(
    DateOnly Desde,
    DateOnly Hasta,
    int TotalUnidades,
    decimal TotalValorUsd,
    IReadOnlyList<FilaFabricante> Filas);

public record FilaFabricante(string Fabricante, int Unidades, decimal ValorUsd, int Ordenes);
