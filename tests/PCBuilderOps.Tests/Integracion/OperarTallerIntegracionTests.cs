using Core.Auth.Domain;
using Core.Permisos.Domain;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Integracion;

// Gestión de permisos y el módulo reales, sin dobles y sin levantar la aplicación.
public sealed class OperarTallerIntegracionTests : IDisposable
{
    private static readonly DatosPrueba Prueba = new("Cinebench", 70m, 65m, 12000, true);

    private readonly BaseDeDatosDelNegocio _bd = new();
    private readonly Guid _administradorId;
    private readonly Guid _solicitanteId;

    public OperarTallerIntegracionTests()
    {
        _administradorId = _bd.CrearUsuario(RolUsuario.Administrador);
        _solicitanteId = _bd.CrearUsuario(RolUsuario.Estandar);
    }

    public void Dispose() => _bd.Dispose();

    private async Task<Guid> OrdenEnPruebasAsync()
    {
        var clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
        var tecnicoAsignadoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);

        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(clienteId,
        [
            new LineaCotizacion(ComponentesIniciales.CpuAm5, 1),
            new LineaCotizacion(ComponentesIniciales.PlacaAm5, 1),
            new LineaCotizacion(ComponentesIniciales.Fuente850W, 1)
        ]);

        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(orden.Id, tecnicoAsignadoId);
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(new ActorTaller(tecnicoAsignadoId, false), orden.Id);

        return orden.Id;
    }

    private Task<int> PruebasGuardadasAsync() =>
        _bd.NuevoContextoNegocio().PruebasRendimiento.CountAsync();

    // RF-GP-05, FN-03
    [Fact]
    public async Task RegistrarPrueba_SeRechazaAntesDeAprobarLaSolicitudYSePermiteDespues()
    {
        // Preparar
        var ordenId = await OrdenEnPruebasAsync();
        var solicitante = new ActorTaller(_solicitanteId, false);
        var solicitud = await _bd.NuevaGestionPermisos().CrearAsync(_solicitanteId, TipoPermiso.OperarTaller);

        // Actuar
        var errorAntes = await Record.ExceptionAsync(() =>
            _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(solicitante, ordenId, Prueba));
        var pruebasAntes = await PruebasGuardadasAsync();

        await _bd.NuevaGestionPermisos().AprobarAsync(solicitud.Id, _administradorId);

        var orden = await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(solicitante, ordenId, Prueba);

        // Verificar
        Assert.IsType<PermisoTallerRequeridoException>(errorAntes);
        Assert.Equal(0, pruebasAntes);
        Assert.Single(orden.Pruebas);
        Assert.Equal(1, await PruebasGuardadasAsync());
    }

    // RF-GP-04, RF-GP-05, FN-03
    [Fact]
    public async Task RegistrarPrueba_ConLaSolicitudRechazada_SigueRechazada()
    {
        // Preparar
        var ordenId = await OrdenEnPruebasAsync();
        var solicitante = new ActorTaller(_solicitanteId, false);
        var solicitud = await _bd.NuevaGestionPermisos().CrearAsync(_solicitanteId, TipoPermiso.OperarTaller);
        await _bd.NuevaGestionPermisos().RechazarAsync(solicitud.Id, _administradorId, "No trabaja en el taller.");

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(solicitante, ordenId, Prueba);

        // Verificar
        await Assert.ThrowsAsync<PermisoTallerRequeridoException>(accion);
        Assert.Equal(0, await PruebasGuardadasAsync());
    }

    // RF-NEG-09: cada máquina avanza por su lado.
    [Fact]
    public async Task AprobarLaSolicitud_NoCambiaElEstadoDeLaOrden()
    {
        // Preparar
        var ordenId = await OrdenEnPruebasAsync();
        var solicitud = await _bd.NuevaGestionPermisos().CrearAsync(_solicitanteId, TipoPermiso.OperarTaller);

        // Actuar
        var aprobada = await _bd.NuevaGestionPermisos().AprobarAsync(solicitud.Id, _administradorId);

        // Verificar
        var orden = await _bd.NuevoContextoNegocio().OrdenesEnsamble.AsNoTracking().SingleAsync(o => o.Id == ordenId);
        Assert.Equal(nameof(EstadoSolicitud.Aplicada), aprobada.Estado);
        Assert.Equal("EnPruebas", orden.Estado.ToString());
    }
}
