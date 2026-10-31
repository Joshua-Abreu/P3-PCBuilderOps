namespace PCBuilderOps.Services;

public interface IOrdenesService
{
    Task<IReadOnlyList<ComponenteResumen>> ListarComponentesAsync(CancellationToken ct = default);
    Task<OrdenDetalle> CotizarAsync(Guid clienteId, IReadOnlyCollection<LineaCotizacion> lineas, CancellationToken ct = default);
    Task<OrdenDetalle> IniciarArmadoAsync(Guid ordenId, Guid tecnicoId, CancellationToken ct = default);
    Task<OrdenDetalle> PasarAPruebasAsync(ActorTaller actor, Guid ordenId, CancellationToken ct = default);
    Task<OrdenDetalle> RegistrarPruebaAsync(ActorTaller actor, Guid ordenId, DatosPrueba datos, CancellationToken ct = default);
    Task<OrdenDetalle> EntregarAsync(ActorTaller actor, Guid ordenId, CancellationToken ct = default);
    Task<IReadOnlyList<OrdenResumen>> ListarDelClienteAsync(Guid clienteId, CancellationToken ct = default);
    Task<OrdenDetalle> ObtenerDelClienteAsync(Guid clienteId, Guid ordenId, CancellationToken ct = default);
    Task<IReadOnlyList<OrdenResumen>> ListarParaTallerAsync(ActorTaller actor, CancellationToken ct = default);
}

public record ActorTaller(Guid UsuarioId, bool EsAdministrador);

public record LineaCotizacion(Guid ComponenteId, int Cantidad);

public record DatosPrueba(
    string? TipoSoftwarePrueba,
    decimal TemperaturaMaximaCpu,
    decimal TemperaturaMaximaGpu,
    int PuntajeBenchmark,
    bool EsAprobada);

public record ComponenteResumen(
    Guid Id,
    string Nombre,
    string Categoria,
    string Fabricante,
    string SocketOInterfaz,
    int ConsumoWatts,
    int? PotenciaWatts,
    decimal Precio,
    int StockDisponible);

public record OrdenResumen(
    Guid Id,
    string CodigoSeguimiento,
    Guid ClienteUsuarioId,
    Guid? TecnicoAsignadoId,
    string Estado,
    decimal CostoTotal,
    DateTime FechaCreacionUtc);

public record DetalleOrden(Guid ComponenteId, string Componente, int Cantidad, decimal PrecioUnitario);

public record PruebaOrden(
    Guid Id,
    string TipoSoftwarePrueba,
    decimal TemperaturaMaximaCpu,
    decimal TemperaturaMaximaGpu,
    int PuntajeBenchmark,
    bool EsAprobada,
    DateTime FechaEjecucionUtc);

public record OrdenDetalle(
    Guid Id,
    string CodigoSeguimiento,
    Guid ClienteUsuarioId,
    Guid? TecnicoAsignadoId,
    string Estado,
    decimal CostoTotal,
    DateTime FechaCreacionUtc,
    IReadOnlyList<DetalleOrden> Detalles,
    IReadOnlyList<PruebaOrden> Pruebas);
