using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public record LineaCotizacionRequest(Guid ComponenteId, int Cantidad);
public record CotizarOrdenRequest(IReadOnlyList<LineaCotizacionRequest> Componentes);
public record IniciarArmadoRequest(Guid TecnicoId);

public record RegistrarPruebaRequest(
    string? TipoSoftwarePrueba,
    decimal TemperaturaMaximaCpu,
    decimal TemperaturaMaximaGpu,
    [Required] int? PuntajeBenchmark,
    [Required] bool? EsAprobada);
