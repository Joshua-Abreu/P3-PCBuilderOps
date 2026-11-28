using Core.Auth.Domain;
using Core.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Domain;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// FN-05, RF-AUD-01, RF-NEG-07, RD-04 · criterios CA-HIS-01 a CA-HIS-03, CA-HIS-06 a CA-HIS-09 y CA-HIS-13
public sealed class HistorialOrdenTests : IDisposable
{
    private static readonly LineaCotizacion[] PcCompatible =
    [
        new(ComponentesIniciales.CpuAm5, 1),
        new(ComponentesIniciales.PlacaAm5, 1),
        new(ComponentesIniciales.GpuMedia, 1),
        new(ComponentesIniciales.Fuente850W, 1)
    ];

    private static readonly DatosPrueba PruebaAprobada = new("Cinebench", 70m, 65m, 12000, true);

    private readonly BaseDeDatosDelNegocio _bd = new();
    private readonly Guid _administradorId;
    private readonly Guid _clienteId;

    public HistorialOrdenTests()
    {
        _administradorId = _bd.CrearUsuario(RolUsuario.Administrador);
        _clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
    }

    public void Dispose() => _bd.Dispose();

    private ActorTaller Administrador => new(_administradorId, true);

    private async Task<(OrdenDetalle Orden, ActorTaller Tecnico)> OrdenEnArmadoAsync()
    {
        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);

        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(_administradorId, orden.Id, tecnicoId);

        return (orden, new ActorTaller(tecnicoId, false));
    }

    private List<TransicionOrden> HistorialGuardado(Guid ordenId) =>
        _bd.NuevoContextoNegocio().TransicionesOrden.AsNoTracking()
            .Where(t => t.OrdenEnsambleId == ordenId)
            .OrderBy(t => t.FechaUtc).ThenBy(t => t.EstadoNuevo)
            .ToList();

    private (int Historial, int Auditoria, int Notificaciones, int Correos) Rastro(Guid ordenId)
    {
        var core = _bd.NuevoContextoCore();
        var correoCliente = core.Usuarios.Single(u => u.Id == _clienteId).Correo;

        return (
            HistorialGuardado(ordenId).Count,
            core.RegistrosAuditoria.Count(r => r.Entidad == AuditoriaOrden.Entidad && r.EntidadId == ordenId.ToString()),
            core.Notificaciones.Count(n => n.UsuarioId == _clienteId),
            core.CorreosEnCola.Count(c => c.Destinatario == correoCliente));
    }

    private async Task<string> EstadoGuardadoAsync(Guid ordenId) =>
        (await _bd.NuevoContextoNegocio().OrdenesEnsamble.AsNoTracking().SingleAsync(o => o.Id == ordenId)).Estado.ToString();

    // CA-HIS-06
    [Fact]
    public async Task Cotizar_AbreElHistorialConLaEntradaDeCreacionDelCliente()
    {
        // Actuar
        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);

        // Verificar
        var entrada = Assert.Single(orden.Historial);
        Assert.Null(entrada.EstadoAnterior);
        Assert.Equal("Cotizada", entrada.EstadoNuevo);
        Assert.Equal(_bd.Reloj.GetUtcNow().UtcDateTime, entrada.FechaUtc);
        var guardada = Assert.Single(HistorialGuardado(orden.Id));
        Assert.Equal(_clienteId, guardada.UsuarioId);
        Assert.Null(guardada.SegundosEnEstadoAnterior);
    }

    // CA-HIS-07
    [Fact]
    public async Task FlujoCompleto_DejaCuatroEntradasConResponsableFechaYTiempoDeArmado()
    {
        // Preparar
        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);
        var tecnico = new ActorTaller(tecnicoId, false);
        var inicio = _bd.Reloj.GetUtcNow().UtcDateTime;

        // Actuar
        _bd.Reloj.Avanzar(TimeSpan.FromMinutes(30));
        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(_administradorId, orden.Id, tecnicoId);
        _bd.Reloj.Avanzar(TimeSpan.FromHours(2));
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(tecnico, orden.Id);
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(tecnico, orden.Id, PruebaAprobada);
        _bd.Reloj.Avanzar(TimeSpan.FromMinutes(45));
        var entregada = await _bd.NuevoServicioOrdenes().EntregarAsync(tecnico, orden.Id);
        var consultada = await _bd.NuevoServicioOrdenes().ObtenerDelClienteAsync(_clienteId, orden.Id);

        // Verificar
        var esperado = new[]
        {
            new EntradaHistorialOrden(null, "Cotizada", inicio),
            new EntradaHistorialOrden("Cotizada", "EnArmado", inicio.AddMinutes(30)),
            new EntradaHistorialOrden("EnArmado", "EnPruebas", inicio.AddMinutes(150)),
            new EntradaHistorialOrden("EnPruebas", "Entregada", inicio.AddMinutes(195))
        };
        Assert.Equal(esperado, entregada.Historial);
        Assert.Equal(esperado, consultada.Historial);
        Assert.All(consultada.Historial, e => Assert.Equal(DateTimeKind.Utc, e.FechaUtc.Kind));

        var guardado = HistorialGuardado(orden.Id);
        Assert.Equal(new[] { _clienteId, _administradorId, tecnicoId, tecnicoId }, guardado.Select(t => t.UsuarioId));
        Assert.Equal(new long?[] { null, 30 * 60, 2 * 60 * 60, 45 * 60 }, guardado.Select(t => t.SegundosEnEstadoAnterior));
    }

    // CA-HIS-08
    [Fact]
    public async Task OtroCliente_NoVeElDetalleNiElHistorial()
    {
        // Preparar
        var (orden, _) = await OrdenEnArmadoAsync();
        var otroClienteId = _bd.CrearUsuario(RolUsuario.Estandar);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().ObtenerDelClienteAsync(otroClienteId, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<OrdenNoEncontradaException>(accion);
    }

    // CA-HIS-09
    [Fact]
    public async Task LaBase_RechazaUnaSegundaEntradaHaciaElMismoEstado()
    {
        // Preparar
        var (orden, tecnico) = await OrdenEnArmadoAsync();
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(tecnico, orden.Id);
        var contexto = _bd.NuevoContextoNegocio();
        contexto.TransicionesOrden.Add(TransicionOrden.Crear(
            orden.Id, EstadoOrdenEnsamble.EnArmado, EstadoOrdenEnsamble.EnPruebas, tecnico.UsuarioId, OrdenesDePrueba.Fecha, null));

        // Actuar
        var accion = () => contexto.SaveChangesAsync();

        // Verificar
        await Assert.ThrowsAsync<DbUpdateException>(accion);
        Assert.Single(HistorialGuardado(orden.Id), t => t.EstadoNuevo == EstadoOrdenEnsamble.EnPruebas);
    }

    // CA-HIS-01
    [Fact]
    public async Task PasarAPruebas_GuardaEstadoHistorialAuditoriaYAvisoJuntos()
    {
        // Preparar
        var (orden, tecnico) = await OrdenEnArmadoAsync();
        var antes = Rastro(orden.Id);

        // Actuar
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(tecnico, orden.Id);

        // Verificar
        Assert.Equal("EnPruebas", await EstadoGuardadoAsync(orden.Id));
        Assert.Equal((antes.Historial + 1, antes.Auditoria + 1, antes.Notificaciones + 1, antes.Correos + 1), Rastro(orden.Id));
    }

    // CA-HIS-02
    [Fact]
    public async Task PasarAPruebas_FallaElNotificadorDespuesDeGuardarLaOrden_NoQuedaNadaDeLaTransicion()
    {
        // Preparar
        var (orden, tecnico) = await OrdenEnArmadoAsync();
        var antes = Rastro(orden.Id);
        var servicio = _bd.NuevoServicioOrdenes(notificador: _ => new NotificadorQueFalla());

        // Actuar
        var accion = () => servicio.PasarAPruebasAsync(tecnico, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        Assert.Equal("EnArmado", await EstadoGuardadoAsync(orden.Id));
        Assert.Equal(antes, Rastro(orden.Id));
    }

    // CA-HIS-03
    [Fact]
    public async Task TransicionesRechazadas_NoDejanHistorialAuditoriaNiAviso()
    {
        // Preparar
        var cotizada = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);
        var (enArmado, tecnico) = await OrdenEnArmadoAsync();
        var sinPermiso = new ActorTaller(_bd.CrearUsuario(RolUsuario.Estandar), false);
        var antesCotizada = Rastro(cotizada.Id);
        var antesEnArmado = Rastro(enArmado.Id);

        // Actuar
        var entregarCotizada = () => _bd.NuevoServicioOrdenes().EntregarAsync(Administrador, cotizada.Id);
        var entregarSinPruebas = () => _bd.NuevoServicioOrdenes().EntregarAsync(tecnico, enArmado.Id);
        var pasarSinPermiso = () => _bd.NuevoServicioOrdenes().PasarAPruebasAsync(sinPermiso, enArmado.Id);

        // Verificar
        await Assert.ThrowsAsync<TransicionOrdenNoPermitidaException>(entregarCotizada);
        await Assert.ThrowsAsync<TransicionOrdenNoPermitidaException>(entregarSinPruebas);
        await Assert.ThrowsAsync<PermisoTallerRequeridoException>(pasarSinPermiso);
        Assert.Equal(antesCotizada, Rastro(cotizada.Id));
        Assert.Equal(antesEnArmado, Rastro(enArmado.Id));
        Assert.Equal("Cotizada", await EstadoGuardadoAsync(cotizada.Id));
        Assert.Equal("EnArmado", await EstadoGuardadoAsync(enArmado.Id));
    }

    // CA-HIS-03
    [Fact]
    public async Task EntregarConLaUltimaPruebaReprobada_YEntregarDosVeces_NoDejanRastro()
    {
        // Preparar
        var (orden, tecnico) = await OrdenEnArmadoAsync();
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(tecnico, orden.Id);
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(tecnico, orden.Id, PruebaAprobada with { EsAprobada = false });
        var antesReprobada = Rastro(orden.Id);
        var reprobada = () => _bd.NuevoServicioOrdenes().EntregarAsync(tecnico, orden.Id);
        await Assert.ThrowsAsync<PruebasNoAprobadasException>(reprobada);
        Assert.Equal(antesReprobada, Rastro(orden.Id));
        _bd.Reloj.Avanzar(TimeSpan.FromMinutes(10));
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(tecnico, orden.Id, PruebaAprobada);
        await _bd.NuevoServicioOrdenes().EntregarAsync(tecnico, orden.Id);
        var antesTerminal = Rastro(orden.Id);

        // Actuar
        var otraVez = () => _bd.NuevoServicioOrdenes().EntregarAsync(tecnico, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<TransicionOrdenNoPermitidaException>(otraVez);
        Assert.Equal(antesTerminal, Rastro(orden.Id));
    }

    // CA-HIS-03
    [Fact]
    public async Task IniciarArmadoSinExistencias_NoDejaRastro()
    {
        // Preparar
        LineaCotizacion[] conGpuEscasa =
        [
            new(ComponentesIniciales.CpuAm5, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.GpuAlta, 1),
            new(ComponentesIniciales.Fuente850W, 1)
        ];
        var primera = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, conGpuEscasa);
        var segunda = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, conGpuEscasa);
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);
        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(_administradorId, primera.Id, tecnicoId);
        var antes = Rastro(segunda.Id);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().IniciarArmadoAsync(_administradorId, segunda.Id, tecnicoId);

        // Verificar
        await Assert.ThrowsAsync<StockInsuficienteException>(accion);
        Assert.Equal(antes, Rastro(segunda.Id));
    }

    // CA-HIS-13
    [Fact]
    public async Task CotizarYRegistrarUnaPrueba_NoSeAuditan()
    {
        // Preparar
        var (orden, tecnico) = await OrdenEnArmadoAsync();
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(tecnico, orden.Id);
        var auditoriaAntes = _bd.NuevoContextoCore().RegistrosAuditoria.Count();

        // Actuar
        var otra = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(tecnico, orden.Id, PruebaAprobada);

        // Verificar
        Assert.Equal(auditoriaAntes, _bd.NuevoContextoCore().RegistrosAuditoria.Count());
        Assert.Single(otra.Historial);
    }

    private sealed class NotificadorQueFalla : INotificador
    {
        public Task NotificarAsync(Guid usuarioId, string asunto, string mensaje, CancellationToken ct = default) =>
            throw new InvalidOperationException("Falla simulada al notificar.");

        public Task NotificarAdministradoresAsync(string asunto, string mensaje, Guid? excepto = null, CancellationToken ct = default) =>
            throw new InvalidOperationException("Falla simulada al notificar.");
    }
}
