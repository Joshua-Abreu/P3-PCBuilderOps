using Core.Auditoria.Services;
using Core.Auth.Exceptions;
using Core.Data;
using Core.Notifications.Services;
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
    private readonly INotificador _notificador;
    private readonly IAuditor _auditor;
    private readonly IOperacionCompartida _operacion;

    public OrdenesService(
        PCBuilderOpsDbContext dbContext,
        IVerificadorPermisos verificadorPermisos,
        TimeProvider reloj,
        INotificador notificador,
        IAuditor auditor,
        IOperacionCompartida operacion)
    {
        _dbContext = dbContext;
        _verificadorPermisos = verificadorPermisos;
        _reloj = reloj;
        _notificador = notificador;
        _auditor = auditor;
        _operacion = operacion;
    }

    private DateTime AhoraUtc => _reloj.GetUtcNow().UtcDateTime;

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

    // FN-01
    public async Task<ValidacionArmado> ValidarArmadoAsync(IReadOnlyCollection<LineaCotizacion> lineas, CancellationToken ct = default)
    {
        var componentes = await ResolverComponentesAsync(lineas, ct);
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);
        var problemas = resultado.Problemas.ToList();

        if (componentes.Any(c => !DetalleEnsamble.CantidadValida(c.Cantidad)))
            problemas.Add(DetalleEnsamble.MensajeCantidadInvalida);

        return new ValidacionArmado(
            resultado.Faltantes.Count == 0 && problemas.Count == 0,
            resultado.Faltantes.Select(c => c.ToString()).ToList(),
            problemas,
            resultado.Advertencias,
            resultado.ConsumoWatts,
            resultado.PotenciaWatts,
            componentes.Sum(c => c.Componente.Precio * c.Cantidad));
    }

    // FN-02
    public async Task<OrdenDetalle> IniciarArmadoAsync(Guid administradorId, Guid ordenId, Guid tecnicoId, CancellationToken ct = default)
    {
        var orden = await ObtenerAsync(ordenId, ct);

        if (!await _verificadorPermisos.TienePermisoAsync(tecnicoId, TipoPermiso.OperarTaller, ct))
            throw new ValidacionException("El técnico indicado no tiene el permiso OperarTaller.");

        await TransicionarAsync(
            orden,
            administradorId,
            AuditoriaOrden.ArmadoIniciado,
            () => orden.IniciarArmado(tecnicoId, administradorId, AhoraUtc),
            o => new { estado = o.Estado.ToString(), tecnicoAsignadoId = o.TecnicoAsignadoId },
            aviso: null,
            ct);

        return ADetalle(orden);
    }

    // FN-04
    public async Task<OrdenDetalle> PasarAPruebasAsync(ActorTaller actor, Guid ordenId, CancellationToken ct = default)
    {
        await ExigirPermisoTallerAsync(actor, ct);

        var orden = await ObtenerAsync(ordenId, ct);

        await TransicionarAsync(
            orden,
            actor.UsuarioId,
            AuditoriaOrden.OrdenEnPruebas,
            () => orden.PasarAPruebas(actor.UsuarioId, AhoraUtc),
            SoloEstado,
            // RF-NEG-07, RF-NOT-03
            o => new AvisoCliente(
                $"Tu orden {o.CodigoSeguimiento} está en pruebas",
                $"La orden {o.CodigoSeguimiento} pasó al estado {o.Estado}: el armado terminó y el equipo está en pruebas de estrés y benchmarking."),
            ct);

        return ADetalle(orden);
    }

    // FN-03, RF-GP-05
    public async Task<OrdenDetalle> RegistrarPruebaAsync(ActorTaller actor, Guid ordenId, DatosPrueba datos, CancellationToken ct = default)
    {
        await ExigirPermisoTallerAsync(actor, ct);

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

        await TransicionarAsync(
            orden,
            actor.UsuarioId,
            AuditoriaOrden.OrdenEntregada,
            () => orden.Entregar(actor.UsuarioId, AhoraUtc),
            SoloEstado,
            // RF-NEG-07, RF-NOT-03
            o => new AvisoCliente(
                $"Tu orden {o.CodigoSeguimiento} fue entregada",
                $"La orden {o.CodigoSeguimiento} pasó al estado {o.Estado}: el equipo aprobó sus pruebas y quedó entregado."),
            ct);

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

    // RF-AUD-01, RF-NOT-03, CA-HIS-01 a CA-HIS-04
    private async Task TransicionarAsync(
        OrdenEnsamble orden,
        Guid usuarioId,
        string accion,
        Action transicionar,
        Func<OrdenEnsamble, object> valoresAuditados,
        Func<OrdenEnsamble, AvisoCliente>? aviso,
        CancellationToken ct)
    {
        var valorAnterior = valoresAuditados(orden);

        transicionar();

        await _operacion.EjecutarAsync(_dbContext, async enOperacion =>
        {
            await GuardarTransicionAsync(orden.Id, orden.Estado, enOperacion);

            _auditor.Registrar(usuarioId, accion, AuditoriaOrden.Entidad, orden.Id.ToString(), valorAnterior, valoresAuditados(orden));

            if (aviso?.Invoke(orden) is { } avisoCliente)
                await _notificador.NotificarAsync(orden.ClienteUsuarioId, avisoCliente.Asunto, avisoCliente.Mensaje, enOperacion);

            return orden.Estado;
        }, ct);
    }

    private static object SoloEstado(OrdenEnsamble orden) => new { estado = orden.Estado.ToString() };

    private sealed record AvisoCliente(string Asunto, string Mensaje);

    // Hallazgo 1 (Asignación 3), RF-NEG-04
    private async Task GuardarTransicionAsync(Guid ordenId, EstadoOrdenEnsamble hacia, CancellationToken ct)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            var estadoGuardado = await _dbContext.OrdenesEnsamble
                .AsNoTracking()
                .Where(o => o.Id == ordenId)
                .Select(o => o.Estado)
                .SingleAsync(ct);

            throw new TransicionOrdenNoPermitidaException(estadoGuardado, hacia);
        }
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

        if (componentes.Values.FirstOrDefault(c => !c.EstaActivo) is { } retirado)
            throw new ValidacionException($"El componente {retirado.Nombre} ya no está disponible.");

        return lineas.Select(l => (componentes[l.ComponenteId], l.Cantidad)).ToList();
    }

    private async Task<OrdenEnsamble> ObtenerAsync(Guid ordenId, CancellationToken ct) =>
        await _dbContext.OrdenesEnsamble
            .Include(o => o.Detalles).ThenInclude(d => d.Componente)
            .Include(o => o.Pruebas)
            .Include(o => o.Historial)
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
            .ToList(),
        o.Historial
            .OrderBy(t => t.FechaUtc)
            .ThenBy(t => t.EstadoNuevo)
            .Select(t => new EntradaHistorialOrden(t.EstadoAnterior?.ToString(), t.EstadoNuevo.ToString(), t.FechaUtc))
            .ToList());
}
