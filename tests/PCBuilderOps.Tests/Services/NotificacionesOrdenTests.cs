using Core.Auth.Domain;
using Core.Notifications.Domain;
using Core.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// RF-NEG-07, RF-NOT-03
public sealed class NotificacionesOrdenTests : IDisposable
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
    private readonly Guid _otroClienteId;

    public NotificacionesOrdenTests()
    {
        _administradorId = _bd.CrearUsuario(RolUsuario.Administrador);
        _clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
        _otroClienteId = _bd.CrearUsuario(RolUsuario.Estandar);
    }

    public void Dispose() => _bd.Dispose();

    private async Task<(OrdenDetalle Orden, ActorTaller Tecnico)> OrdenEnArmadoAsync()
    {
        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);
        var tecnicoId = await _bd.CrearTecnicoConPermisoAsync(_administradorId);

        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(orden.Id, tecnicoId);

        return (orden, new ActorTaller(tecnicoId, false));
    }

    private List<Notificacion> NotificacionesDe(Guid usuarioId) =>
        _bd.NuevoContextoCore().Notificaciones.AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId)
            .OrderBy(n => n.FechaCreacionUtc)
            .ToList();

    private List<CorreoEnCola> CorreosDe(Guid usuarioId)
    {
        var contexto = _bd.NuevoContextoCore();
        var correo = contexto.Usuarios.Single(u => u.Id == usuarioId).Correo;
        return contexto.CorreosEnCola.AsNoTracking().Where(c => c.Destinatario == correo).ToList();
    }

    private async Task<string> EstadoGuardadoAsync(Guid ordenId) =>
        (await _bd.NuevoContextoNegocio().OrdenesEnsamble.AsNoTracking().SingleAsync(o => o.Id == ordenId)).Estado.ToString();

    // CA-NOT-04 · RF-NOT-03
    [Fact]
    public async Task PasarAPruebasYEntregar_NotificanAlClienteConElCodigoYElEstadoNuevo()
    {
        // Preparar
        var (orden, tecnico) = await OrdenEnArmadoAsync();

        // Actuar
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(tecnico, orden.Id);
        _bd.Reloj.Avanzar(TimeSpan.FromHours(2));
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(tecnico, orden.Id, PruebaAprobada);
        await _bd.NuevoServicioOrdenes().EntregarAsync(tecnico, orden.Id);

        // Verificar
        var notificaciones = NotificacionesDe(_clienteId);
        Assert.Equal(2, notificaciones.Count);
        Assert.All(notificaciones, n => Assert.Contains(orden.CodigoSeguimiento, n.Mensaje));
        Assert.All(notificaciones, n => Assert.False(n.EsLeida));
        Assert.Contains("EnPruebas", notificaciones[0].Mensaje);
        Assert.Contains("Entregada", notificaciones[1].Mensaje);
        Assert.Empty(NotificacionesDe(_otroClienteId));
    }

    // CA-NOT-04, CA-NOT-10 · RF-NOT-06
    [Fact]
    public async Task PasarAPruebas_EncolaElCorreoDelCliente()
    {
        // Preparar
        var (orden, tecnico) = await OrdenEnArmadoAsync();

        // Actuar
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(tecnico, orden.Id);

        // Verificar
        var notificacion = Assert.Single(NotificacionesDe(_clienteId));
        var correo = Assert.Single(CorreosDe(_clienteId));
        Assert.Equal(notificacion.Asunto, correo.Asunto);
        Assert.Equal(notificacion.Mensaje, correo.Cuerpo);
        Assert.Empty(CorreosDe(_otroClienteId));
    }

    // CA-NOT-11 · RF-NOT-07
    [Fact]
    public async Task PasarAPruebas_ConElCorreoDesactivado_SoloNotificaEnLaBandeja()
    {
        // Preparar
        var (orden, tecnico) = await OrdenEnArmadoAsync();
        await new NotificacionesService(_bd.NuevoContextoCore(), _bd.Reloj).ActualizarPreferenciasAsync(_clienteId, correoActivo: false);

        // Actuar
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(tecnico, orden.Id);

        // Verificar
        Assert.Single(NotificacionesDe(_clienteId));
        Assert.Empty(CorreosDe(_clienteId));
    }

    // CA-NOT-05 · RF-NOT-03
    [Fact]
    public async Task Entregar_OrdenCotizada_SeRechazaYNoNotifica()
    {
        // Preparar
        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);
        var administrador = new ActorTaller(_administradorId, true);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().EntregarAsync(administrador, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<TransicionOrdenNoPermitidaException>(accion);
        Assert.Equal("Cotizada", await EstadoGuardadoAsync(orden.Id));
        Assert.Empty(NotificacionesDe(_clienteId));
        Assert.Empty(CorreosDe(_clienteId));
    }

    // CA-NOT-05 · RF-NOT-03
    [Fact]
    public async Task Entregar_ConLaUltimaPruebaReprobada_SeRechazaYSoloQuedaLaNotificacionDePruebas()
    {
        // Preparar
        var (orden, tecnico) = await OrdenEnArmadoAsync();
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(tecnico, orden.Id);
        await _bd.NuevoServicioOrdenes().RegistrarPruebaAsync(tecnico, orden.Id, PruebaReprobada);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().EntregarAsync(tecnico, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<PruebasNoAprobadasException>(accion);
        Assert.Equal("EnPruebas", await EstadoGuardadoAsync(orden.Id));
        Assert.Contains("EnPruebas", Assert.Single(NotificacionesDe(_clienteId)).Mensaje);
    }

    // CA-NOT-05 · RF-NOT-03
    [Fact]
    public async Task PasarAPruebas_ActorSinPermiso_SeRechazaYNoNotifica()
    {
        // Preparar
        var (orden, _) = await OrdenEnArmadoAsync();
        var sinPermiso = new ActorTaller(_otroClienteId, false);

        // Actuar
        var accion = () => _bd.NuevoServicioOrdenes().PasarAPruebasAsync(sinPermiso, orden.Id);

        // Verificar
        await Assert.ThrowsAsync<PermisoTallerRequeridoException>(accion);
        Assert.Equal("EnArmado", await EstadoGuardadoAsync(orden.Id));
        Assert.Empty(NotificacionesDe(_clienteId));
    }
}
