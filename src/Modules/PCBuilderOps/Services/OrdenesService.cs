using Core.Auth.Exceptions;
using Core.Permisos.Domain;
using Core.Permisos.Services;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Domain;
using PCBuilderOps.Exceptions;

namespace PCBuilderOps.Services;

public class OrdenesService : IOrdenesService
{
    private readonly PCBuilderOpsDbContext _dbContext;
    private readonly IVerificadorPermisos _verificadorPermisos;
    private readonly TimeProvider _reloj;

    public OrdenesService(PCBuilderOpsDbContext dbContext, IVerificadorPermisos verificadorPermisos, TimeProvider reloj)
    {
        _dbContext = dbContext;
        _verificadorPermisos = verificadorPermisos;
        _reloj = reloj;
    }

    private DateTime AhoraUtc => _reloj.GetUtcNow().UtcDateTime;

    // FN-01
    public async Task<IReadOnlyList<ComponenteResumen>> ListarComponentesAsync(CancellationToken ct = default)
    {
        var componentes = await _dbContext.Componentes
            .AsNoTracking()
            .OrderBy(c => c.Categoria)
            .ThenBy(c => c.Nombre)
            .ToListAsync(ct);

        return componentes.Select(c => new ComponenteResumen(
            c.Id,
            c.Nombre,
            c.Categoria.ToString(),
            c.Fabricante,
            c.SocketOInterfaz,
            c.ConsumoWatts,
            c.PotenciaWatts,
            c.Precio,
            c.StockDisponible)).ToList();
    }

    // FN-01
    public async Task<OrdenDetalle> CotizarAsync(Guid clienteId, IReadOnlyCollection<LineaCotizacion> lineas, CancellationToken ct = default)
    {
        var componentes = await ResolverComponentesAsync(lineas, ct);

        VerificadorCompatibilidad.Validar(componentes);

        var orden = OrdenEnsamble.Cotizar(clienteId, componentes, AhoraUtc);

        _dbContext.OrdenesEnsamble.Add(orden);
        await _dbContext.SaveChangesAsync(ct);

        return ADetalle(orden);
    }

    // FN-02
    public async Task<OrdenDetalle> IniciarArmadoAsync(Guid ordenId, Guid tecnicoId, CancellationToken ct = default)
    {
        var orden = await ObtenerAsync(ordenId, ct);

        if (!await _verificadorPermisos.TienePermisoAsync(tecnicoId, TipoPermiso.OperarTaller, ct))
            throw new ValidacionException("El técnico indicado no tiene el permiso OperarTaller.");

        orden.IniciarArmado(tecnicoId);
        await _dbContext.SaveChangesAsync(ct);

        return ADetalle(orden);
    }

    // FN-04
    public async Task<OrdenDetalle> PasarAPruebasAsync(ActorTaller actor, Guid ordenId, CancellationToken ct = default)
    {
        await ExigirPermisoTallerAsync(actor, ct);

        var orden = await ObtenerAsync(ordenId, ct);

        orden.PasarAPruebas();
        await _dbContext.SaveChangesAsync(ct);

        return ADetalle(orden);
    }

    // FN-03, RF-GP-05
    public async Task<OrdenDetalle> RegistrarPruebaAsync(ActorTaller actor, Guid ordenId, DatosPrueba datos, CancellationToken ct = default)
    {

        var orden = await ObtenerAsync(ordenId, ct);

        orden.RegistrarPrueba(
            datos.TipoSoftwarePrueba,
            datos.TemperaturaMaximaCpu,
            datos.TemperaturaMaximaGpu,
            datos.PuntajeBenchmark,
            datos.EsAprobada,
            AhoraUtc);

        await _dbContext.SaveChangesAsync(ct);

        return ADetalle(orden);
    }

    // FN-04
    public async Task<OrdenDetalle> EntregarAsync(ActorTaller actor, Guid ordenId, CancellationToken ct = default)
    {
        await ExigirPermisoTallerAsync(actor, ct);

        var orden = await ObtenerAsync(ordenId, ct);

        orden.Entregar();
        await _dbContext.SaveChangesAsync(ct);

        return ADetalle(orden);
    }

    // FN-05
    public async Task<IReadOnlyList<OrdenResumen>> ListarDelClienteAsync(Guid clienteId, CancellationToken ct = default)
    {
        var ordenes = await _dbContext.OrdenesEnsamble
            .AsNoTracking()
            .Where(o => o.ClienteUsuarioId == clienteId)
            .OrderByDescending(o => o.FechaCreacionUtc)
            .ToListAsync(ct);

        return ordenes.Select(AResumen).ToList();
    }

    // FN-05, RF-NEG-06
    public async Task<OrdenDetalle> ObtenerDelClienteAsync(Guid clienteId, Guid ordenId, CancellationToken ct = default)
    {
        var orden = await ObtenerAsync(ordenId, ct);

        if (orden.ClienteUsuarioId != clienteId)
            throw new OrdenNoEncontradaException();

        return ADetalle(orden);
    }

    // RF-GP-05
    public async Task<IReadOnlyList<OrdenResumen>> ListarParaTallerAsync(ActorTaller actor, CancellationToken ct = default)
    {
        await ExigirPermisoTallerAsync(actor, ct);

        var ordenes = await _dbContext.OrdenesEnsamble
            .AsNoTracking()
            .OrderByDescending(o => o.FechaCreacionUtc)
            .ToListAsync(ct);

        return ordenes.Select(AResumen).ToList();
    }

    // RF-GP-05, RF-NEG-06
    private async Task ExigirPermisoTallerAsync(ActorTaller actor, CancellationToken ct)
    {
        if (actor.EsAdministrador)
            return;

        if (!await _verificadorPermisos.TienePermisoAsync(actor.UsuarioId, TipoPermiso.OperarTaller, ct))
            throw new PermisoTallerRequeridoException();
    }

    private async Task<List<(Componente Componente, int Cantidad)>> ResolverComponentesAsync(
        IReadOnlyCollection<LineaCotizacion> lineas,
        CancellationToken ct)
    {
        var ids = lineas.Select(l => l.ComponenteId).ToList();

        if (ids.Distinct().Count() != ids.Count)
            throw new ValidacionException("Cada componente debe aparecer una sola vez en la cotización.");

        var componentes = await _dbContext.Componentes
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        if (componentes.Count != ids.Count)
            throw new ValidacionException("La cotización incluye un componente que no existe.");

        return lineas.Select(l => (componentes[l.ComponenteId], l.Cantidad)).ToList();
    }

    private async Task<OrdenEnsamble> ObtenerAsync(Guid ordenId, CancellationToken ct) =>
        await _dbContext.OrdenesEnsamble
            .Include(o => o.Detalles).ThenInclude(d => d.Componente)
            .Include(o => o.Pruebas)
            .FirstOrDefaultAsync(o => o.Id == ordenId, ct)
            ?? throw new OrdenNoEncontradaException();

    private static OrdenResumen AResumen(OrdenEnsamble o) => new(
        o.Id,
        o.CodigoSeguimiento,
        o.ClienteUsuarioId,
        o.TecnicoAsignadoId,
        o.Estado.ToString(),
        o.CostoTotal,
        o.FechaCreacionUtc);

    private static OrdenDetalle ADetalle(OrdenEnsamble o) => new(
        o.Id,
        o.CodigoSeguimiento,
        o.ClienteUsuarioId,
        o.TecnicoAsignadoId,
        o.Estado.ToString(),
        o.CostoTotal,
        o.FechaCreacionUtc,
        o.Detalles.Select(d => new DetalleOrden(d.ComponenteId, d.Componente.Nombre, d.Cantidad, d.PrecioUnitario)).ToList(),
        o.Pruebas
            .OrderBy(p => p.FechaEjecucionUtc)
            .Select(p => new PruebaOrden(
                p.Id,
                p.TipoSoftwarePrueba,
                p.TemperaturaMaximaCpu,
                p.TemperaturaMaximaGpu,
                p.PuntajeBenchmark,
                p.EsAprobada,
                p.FechaEjecucionUtc))
            .ToList());
}
