using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// RF-NEG-02: cada funcionalidad se prueba por separado.
public sealed class OrdenesServiceTests : IDisposable
{
    private static readonly LineaCotizacion[] PcCompatible =
    [
        new(ComponentesIniciales.CpuAm5, 1),
        new(ComponentesIniciales.PlacaAm5, 1),
        new(ComponentesIniciales.GpuMedia, 1),
        new(ComponentesIniciales.Fuente850W, 1)
    ];

    private static readonly DatosPrueba PruebaAprobada = new("Cinebench", 70m, 65m, 12000, true);
    private static readonly DatosPrueba PruebaReprobada = new("Cinebench", 98m, 65m, 12000, false);

    private readonly BaseDeDatosDelNegocio _bd = new();
    private readonly Guid _administradorId;
    private readonly Guid _clienteId;

    public OrdenesServiceTests()
    {
        _administradorId = _bd.CrearUsuario(RolUsuario.Administrador);
        _clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
    }

    public void Dispose() => _bd.Dispose();

    private ActorTaller Administrador => new(_administradorId, true);

    private Task<OrdenDetalle> CotizarAsync() => _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);

    private async Task<OrdenDetalle> OrdenEnPruebasAsync()
    {
        var orden = await CotizarAsync();
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);

        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(orden.Id, tecnicoId);

        return await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(Administrador, orden.Id);
    }

    private async Task<string> EstadoGuardadoAsync(Guid ordenId)
    {
        var orden = await _bd.NuevoContextoNegocio().OrdenesEnsamble.AsNoTracking().SingleAsync(o => o.Id == ordenId);
        return orden.Estado.ToString();
    }

    private async Task<int> StockGuardadoAsync(Guid componenteId)
    {
        var componente = await _bd.NuevoContextoNegocio().Componentes.AsNoTracking().SingleAsync(c => c.Id == componenteId);
        return componente.StockDisponible;
    }

    // FN-01
    [Fact]
    public async Task ListarComponentes_DevuelveElCatalogoInicial()
    {
        // Preparar
        var servicio = _bd.NuevoServicioOrdenes();

        // Actuar
        var componentes = await servicio.ListarComponentesAsync();

        // Verificar
        Assert.Equal(ComponentesIniciales.Todos.Length, componentes.Count);
    }

    // FN-01
    [Fact]
    public async Task Cotizar_PcCompatible_GuardaLaOrdenCotizadaConSusDetalles()
    {
        // Preparar
        var servicio = _bd.NuevoServicioOrdenes();

        // Actuar
        var orden = await servicio.CotizarAsync(_clienteId, PcCompatible);

        // Verificar
        var guardada = await _bd.NuevoContextoNegocio().OrdenesEnsamble
            .Include(o => o.Detalles)
            .AsNoTracking()
            .SingleAsync(o => o.Id == orden.Id);

        Assert.Equal("Cotizada", guardada.Estado.ToString());
        Assert.Equal(_clienteId, guardada.ClienteUsuarioId);
        Assert.Equal(4, guardada.Detalles.Count);
        Assert.Equal(299m + 189m + 299m + 139m, guardada.CostoTotal);
    }

    // FN-01
    [Fact]
    public async Task Cotizar_SocketsIncompatibles_SeRechazaYNoGuardaNada()
    {
        // Preparar
        LineaCotizacion[] lineas =
        [
            new(ComponentesIniciales.CpuLga1700, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.Fuente850W, 1)
        ];

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, lineas);

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.Empty(await _bd.NuevoContextoNegocio().OrdenesEnsamble.ToListAsync());
    }

    // FN-01
    [Fact]
    public async Task Cotizar_ComponenteQueNoExiste_SeRechaza()
    {
        // Preparar
        LineaCotizacion[] lineas = [new(Guid.NewGuid(), 1)];

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, lineas);

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
    }

    // FN-01
    [Fact]
    public async Task Cotizar_ComponenteRepetido_SeRechaza()
    {
        // Preparar
        LineaCotizacion[] lineas = [PcCompatible[0], PcCompatible[0]];

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, lineas);

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
    }

    // FN-02
    [Fact]
    public async Task IniciarArmado_TecnicoConPermiso_GuardaEnArmadoConTecnicoYStockReservado()
    {
        // Preparar
        var orden = await CotizarAsync();
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);
        var stockAntes = await StockGuardadoAsync(ComponentesIniciales.CpuAm5);

        // Actuar
        var resultado = await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(orden.Id, tecnicoId);

        // Verificar
        Assert.Equal("EnArmado", await EstadoGuardadoAsync(orden.Id));
        Assert.Equal(tecnicoId, resultado.TecnicoAsignadoId);
        Assert.Equal(stockAntes - 1, await StockGuardadoAsync(ComponentesIniciales.CpuAm5));
    }

    // FN-02: condición de la primera fila de docs/maquina-de-estados.md
    [Fact]
    public async Task IniciarArmado_TecnicoSinPermiso_SeRechazaYLaOrdenSigueCotizada()
    {
        // Preparar
        var orden = await CotizarAsync();
        var tecnicoSinPermisoId = _bd.CrearUsuario(RolUsuario.Estandar);
        var stockAntes = await StockGuardadoAsync(ComponentesIniciales.CpuAm5);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().IniciarArmadoAsync(orden.Id, tecnicoSinPermisoId);

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.Equal("Cotizada", await EstadoGuardadoAsync(orden.Id));
        Assert.Equal(stockAntes, await StockGuardadoAsync(ComponentesIniciales.CpuAm5));
    }

    // FN-02
    [Fact]
    public async Task IniciarArmado_SinStockSuficiente_SeRechazaYNoGuardaNingunCambio()
    {
        // Preparar
        LineaCotizacion[] lineas =
        [
            new(ComponentesIniciales.CpuAm5, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.GpuAlta, 1),
            new(ComponentesIniciales.Fuente850W, 1)
        ];
        var primera = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, lineas);
        var segunda = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, lineas);
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);
        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(primera.Id, tecnicoId);
        var stockCpuAntes = await StockGuardadoAsync(ComponentesIniciales.CpuAm5);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().IniciarArmadoAsync(segunda.Id, tecnicoId);

        // Verificar
        await Assert.ThrowsAsync<StockInsuficienteException>(accion);
        Assert.Equal("Cotizada", await EstadoGuardadoAsync(segunda.Id));
        Assert.Equal(stockCpuAntes, await StockGuardadoAsync(ComponentesIniciales.CpuAm5));
    }

    // FN-02
    [Fact]
    public async Task IniciarArmado_OrdenQueNoExiste_SeRechaza()
    {
        // Preparar
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().IniciarArmadoAsync(Guid.NewGuid(), tecnicoId);

        // Verificar
        await Assert.ThrowsAsync<OrdenNoEncontradaException>(accion);
    }

    // FN-03
    [Fact]
    public async Task RegistrarPrueba_ActorConPermiso_GuardaLaPruebaConLaFechaDelReloj()
    {
        // Preparar
        var orden = await OrdenEnPruebasAsync();
        var tecnico = new ActorTaller(await _bd.CrearTecnicoConPermisoAsync(_administradorId), false);

        // Actuar
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(tecnico, orden.Id, PruebaAprobada);

        // Verificar
        var prueba = await _bd.NuevoContextoNegocio().PruebasRendimiento.AsNoTracking().SingleAsync();
        Assert.Equal(orden.Id, prueba.OrdenEnsambleId);
        Assert.True(prueba.EsAprobada);
        Assert.Equal(_bd.Reloj.GetUtcNow().UtcDateTime, prueba.FechaEjecucionUtc);
    }

    // FN-04
    [Fact]
    public async Task PasarAPruebas_ActorSinPermiso_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var orden = await CotizarAsync();
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);
        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(orden.Id, tecnicoId);
        var sinPermiso = new ActorTaller(_bd.CrearUsuario(RolUsuario.Estandar), false);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().PasarAPruebasAsync(sinPermiso, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<PermisoTallerRequeridoException>(accion);
        Assert.Equal("EnArmado", await EstadoGuardadoAsync(orden.Id));
    }

    // FN-04
    [Fact]
    public async Task Entregar_ActorSinPermiso_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var orden = await OrdenEnPruebasAsync();
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(Administrador, orden.Id, PruebaAprobada);
        var sinPermiso = new ActorTaller(_bd.CrearUsuario(RolUsuario.Estandar), false);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().EntregarAsync(sinPermiso, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<PermisoTallerRequeridoException>(accion);
        Assert.Equal("EnPruebas", await EstadoGuardadoAsync(orden.Id));
    }

    // FN-04
    [Fact]
    public async Task Entregar_TrasCorregirUnaPruebaReprobada_GuardaEntregadaYConservaElHistorial()
    {
        // Preparar
        var orden = await OrdenEnPruebasAsync();
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(Administrador, orden.Id, PruebaReprobada);
        _bd.Reloj.Avanzar(TimeSpan.FromHours(1));
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(Administrador, orden.Id, PruebaAprobada);

        // Actuar
        var resultado = await _bd.NuevoServicioOrdenes().EntregarAsync(Administrador, orden.Id);

        // Verificar
        Assert.Equal("Entregada", await EstadoGuardadoAsync(orden.Id));
        Assert.Equal(2, resultado.Pruebas.Count);
    }

    // FN-04
    [Fact]
    public async Task Entregar_ConLaUltimaPruebaReprobada_SeRechazaYElEstadoGuardadoNoCambia()
    {
        // Preparar
        var orden = await OrdenEnPruebasAsync();
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(Administrador, orden.Id, PruebaReprobada);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().EntregarAsync(Administrador, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<PruebasNoAprobadasException>(accion);
        Assert.Equal("EnPruebas", await EstadoGuardadoAsync(orden.Id));
    }

    // FN-05
    [Fact]
    public async Task ObtenerDelCliente_OrdenPropia_DevuelveSuDetalle()
    {
        // Preparar
        var orden = await CotizarAsync();

        // Actuar
        var detalle = await _bd.NuevoServicioOrdenes().ObtenerDelClienteAsync(_clienteId, orden.Id);

        // Verificar
        Assert.Equal(orden.Id, detalle.Id);
        Assert.Equal(4, detalle.Detalles.Count);
    }

    // FN-05, RF-NEG-06
    [Fact]
    public async Task ObtenerDelCliente_OrdenDeOtroCliente_SeRechazaComoNoEncontrada()
    {
        // Preparar
        var orden = await CotizarAsync();
        var otroClienteId = _bd.CrearUsuario(RolUsuario.Estandar);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().ObtenerDelClienteAsync(otroClienteId, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<OrdenNoEncontradaException>(accion);
    }

    // FN-05, RF-NEG-06
    [Fact]
    public async Task ListarDelCliente_SoloDevuelveLasOrdenesPropias()
    {
        // Preparar
        var propia = await CotizarAsync();
        var otroClienteId = _bd.CrearUsuario(RolUsuario.Estandar);
        await _bd.NuevoServicioOrdenes().CotizarAsync(otroClienteId, PcCompatible);

        // Actuar
        var ordenes = await _bd.NuevoServicioOrdenes().ListarDelClienteAsync(_clienteId);

        // Verificar
        Assert.Equal(propia.Id, Assert.Single(ordenes).Id);
    }

    // RF-GP-05
    [Fact]
    public async Task ListarParaTaller_ActorSinPermiso_SeRechaza()
    {
        // Preparar
        var sinPermiso = new ActorTaller(_clienteId, false);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().ListarParaTallerAsync(sinPermiso);

        // Verificar
        await Assert.ThrowsAsync<PermisoTallerRequeridoException>(accion);
    }

    // RF-GP-05
    [Fact]
    public async Task ListarParaTaller_Administrador_NoNecesitaElPermiso()
    {
        // Preparar
        var orden = await CotizarAsync();

        // Actuar
        var ordenes = await _bd.NuevoServicioOrdenes().ListarParaTallerAsync(Administrador);

        // Verificar
        Assert.Equal(orden.Id, Assert.Single(ordenes).Id);
    }
}
