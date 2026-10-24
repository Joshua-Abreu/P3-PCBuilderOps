using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Permisos.Domain;
using Core.Permisos.Exceptions;
using Core.Permisos.Services;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Permisos;

public sealed class GestionPermisosServiceTests : IDisposable
{
    private static readonly DateTime FechaCreacion = new(2026, 10, 18, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaResolucion = new(2026, 10, 18, 15, 30, 0, DateTimeKind.Utc);

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly RelojFijo _reloj = new(FechaCreacion);
    private readonly Guid _estandarId;
    private readonly Guid _administradorId;

    public GestionPermisosServiceTests()
    {
        _estandarId = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
        _administradorId = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private GestionPermisosService NuevoServicio() => new(_baseDeDatos.NuevoContexto(), _reloj);

    private async Task<Guid> CrearSolicitudPendienteAsync(Guid solicitanteId)
    {
        var creada = await NuevoServicio().CrearAsync(solicitanteId, TipoPermiso.OperarTaller);
        _reloj.Fijar(FechaResolucion);
        return creada.Id;
    }

    private SolicitudPermiso LeerGuardada(Guid solicitudId)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return contexto.SolicitudesPermiso.AsNoTracking().Single(s => s.Id == solicitudId);
    }

    private int ContarPermisosOtorgados(Guid usuarioId)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return contexto.PermisosUsuario.Count(p => p.UsuarioId == usuarioId);
    }

    // RF-GP-01
    [Fact]
    public async Task Crear_SolicitudNueva_SeGuardaEnPendienteConSolicitanteYFecha()
    {
        // Preparar
        var servicio = NuevoServicio();

        // Actuar
        var creada = await servicio.CrearAsync(_estandarId, TipoPermiso.OperarTaller);

        // Verificar
        var guardada = LeerGuardada(creada.Id);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada.Estado);
        Assert.Equal(_estandarId, guardada.SolicitanteId);
        Assert.Equal(TipoPermiso.OperarTaller, guardada.Permiso);
        Assert.Equal(FechaCreacion, guardada.FechaCreacionUtc);
        Assert.Null(guardada.AprobadorId);
    }

    // RF-GP-01
    [Fact]
    public async Task Crear_ConSolicitudPendienteDelMismoPermiso_SeRechazaYNoGuardaOtra()
    {
        // Preparar
        await NuevoServicio().CrearAsync(_estandarId, TipoPermiso.OperarTaller);

        // Actuar
        var accion = () => NuevoServicio().CrearAsync(_estandarId, TipoPermiso.OperarTaller);

        // Verificar
        await Assert.ThrowsAsync<SolicitudDuplicadaException>(accion);
        using var contexto = _baseDeDatos.NuevoContexto();
        Assert.Equal(1, contexto.SolicitudesPermiso.Count(s => s.SolicitanteId == _estandarId));
    }

    // RF-GP-01
    [Fact]
    public async Task Crear_PermisoYaOtorgado_SeRechaza()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_estandarId);
        await NuevoServicio().AprobarAsync(solicitudId, _administradorId);

        // Actuar
        var accion = () => NuevoServicio().CrearAsync(_estandarId, TipoPermiso.OperarTaller);

        // Verificar
        await Assert.ThrowsAsync<SolicitudDuplicadaException>(accion);
    }

    // RF-GP-03, RF-GP-05
    [Fact]
    public async Task Aprobar_SolicitudPendiente_QuedaAplicadaYGuardaAprobadorYFecha()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_estandarId);

        // Actuar
        await NuevoServicio().AprobarAsync(solicitudId, _administradorId);

        // Verificar
        var guardada = LeerGuardada(solicitudId);
        Assert.Equal(EstadoSolicitud.Aplicada, guardada.Estado);
        Assert.Equal(_administradorId, guardada.AprobadorId);
        Assert.Equal(FechaResolucion, guardada.FechaResolucionUtc);
    }

    // RF-GP-05
    [Fact]
    public async Task Aprobar_SolicitudPendiente_OtorgaElPermisoAlSolicitante()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_estandarId);

        // Actuar
        await NuevoServicio().AprobarAsync(solicitudId, _administradorId);

        // Verificar
        Assert.True(await NuevoServicio().TienePermisoAsync(_estandarId, TipoPermiso.OperarTaller));
    }

    // RF-GP-05
    [Fact]
    public async Task TienePermiso_SolicitudSinResolver_DevuelveFalso()
    {
        // Preparar
        await CrearSolicitudPendienteAsync(_estandarId);

        // Actuar
        var tienePermiso = await NuevoServicio().TienePermisoAsync(_estandarId, TipoPermiso.OperarTaller);

        // Verificar
        Assert.False(tienePermiso);
    }

    // RF-GP-04
    [Fact]
    public async Task Rechazar_SolicitudPendienteConMotivo_QuedaRechazadaYGuardaMotivoAprobadorYFecha()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_estandarId);

        // Actuar
        await NuevoServicio().RechazarAsync(solicitudId, _administradorId, "No trabaja en el taller.");

        // Verificar
        var guardada = LeerGuardada(solicitudId);
        Assert.Equal(EstadoSolicitud.Rechazada, guardada.Estado);
        Assert.Equal("No trabaja en el taller.", guardada.MotivoRechazo);
        Assert.Equal(_administradorId, guardada.AprobadorId);
        Assert.Equal(FechaResolucion, guardada.FechaResolucionUtc);
        Assert.Equal(0, ContarPermisosOtorgados(_estandarId));
    }

    // RF-GP-04
    [Fact]
    public async Task Rechazar_SinMotivo_SeRechazaYLaSolicitudGuardadaSigueEnPendiente()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_estandarId);

        // Actuar
        var accion = () => NuevoServicio().RechazarAsync(solicitudId, _administradorId, "   ");

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
        var guardada = LeerGuardada(solicitudId);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada.Estado);
        Assert.Null(guardada.MotivoRechazo);
        Assert.Null(guardada.FechaResolucionUtc);
    }

    // RF-GP-06
    [Fact]
    public async Task Aprobar_DosVecesLaMismaSolicitud_SeRechazaYLoGuardadoNoCambia()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_estandarId);
        await NuevoServicio().AprobarAsync(solicitudId, _administradorId);
        var otroAdministradorId = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
        _reloj.Fijar(FechaResolucion.AddHours(2));

        // Actuar
        var accion = () => NuevoServicio().AprobarAsync(solicitudId, otroAdministradorId);

        // Verificar
        await Assert.ThrowsAsync<TransicionNoPermitidaException>(accion);
        var guardada = LeerGuardada(solicitudId);
        Assert.Equal(EstadoSolicitud.Aplicada, guardada.Estado);
        Assert.Equal(_administradorId, guardada.AprobadorId);
        Assert.Equal(FechaResolucion, guardada.FechaResolucionUtc);
        Assert.Equal(1, ContarPermisosOtorgados(_estandarId));
    }

    // RF-GP-06
    [Fact]
    public async Task Aprobar_SolicitudRechazada_SeRechazaYNoOtorgaElPermiso()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_estandarId);
        await NuevoServicio().RechazarAsync(solicitudId, _administradorId, "No trabaja en el taller.");

        // Actuar
        var accion = () => NuevoServicio().AprobarAsync(solicitudId, _administradorId);

        // Verificar
        await Assert.ThrowsAsync<TransicionNoPermitidaException>(accion);
        var guardada = LeerGuardada(solicitudId);
        Assert.Equal(EstadoSolicitud.Rechazada, guardada.Estado);
        Assert.Equal("No trabaja en el taller.", guardada.MotivoRechazo);
        Assert.Equal(0, ContarPermisosOtorgados(_estandarId));
    }

    // RF-GP-04, RF-GP-06
    [Fact]
    public async Task Rechazar_DosVecesLaMismaSolicitud_SeRechazaYLoGuardadoNoCambia()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_estandarId);
        await NuevoServicio().RechazarAsync(solicitudId, _administradorId, "No trabaja en el taller.");
        _reloj.Fijar(FechaResolucion.AddHours(2));

        // Actuar
        var accion = () => NuevoServicio().RechazarAsync(solicitudId, _administradorId, "Segundo motivo.");

        // Verificar
        await Assert.ThrowsAsync<TransicionNoPermitidaException>(accion);
        var guardada = LeerGuardada(solicitudId);
        Assert.Equal(EstadoSolicitud.Rechazada, guardada.Estado);
        Assert.Equal("No trabaja en el taller.", guardada.MotivoRechazo);
        Assert.Equal(FechaResolucion, guardada.FechaResolucionUtc);
    }

    // RF-GP-07
    [Fact]
    public async Task Aprobar_SolicitudPropiaDelAdministrador_SeRechazaYNoOtorgaElPermiso()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_administradorId);

        // Actuar
        var accion = () => NuevoServicio().AprobarAsync(solicitudId, _administradorId);

        // Verificar
        await Assert.ThrowsAsync<ResolucionPropiaException>(accion);
        Assert.Equal(EstadoSolicitud.Pendiente, LeerGuardada(solicitudId).Estado);
        Assert.Equal(0, ContarPermisosOtorgados(_administradorId));
    }

    // RF-GP-07
    [Fact]
    public async Task Rechazar_SolicitudPropiaDelAdministrador_SeRechazaYSigueEnPendiente()
    {
        // Preparar
        var solicitudId = await CrearSolicitudPendienteAsync(_administradorId);

        // Actuar
        var accion = () => NuevoServicio().RechazarAsync(solicitudId, _administradorId, "Ya no lo necesito.");

        // Verificar
        await Assert.ThrowsAsync<ResolucionPropiaException>(accion);
        var guardada = LeerGuardada(solicitudId);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada.Estado);
        Assert.Null(guardada.MotivoRechazo);
    }

    // RF-GP-02
    [Fact]
    public async Task ListarPendientes_ConSolicitudesResueltas_DevuelveSoloLasPendientes()
    {
        // Preparar
        var otroEstandarId = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
        var pendienteId = await CrearSolicitudPendienteAsync(_estandarId);
        var resueltaId = await CrearSolicitudPendienteAsync(otroEstandarId);
        await NuevoServicio().AprobarAsync(resueltaId, _administradorId);

        // Actuar
        var pendientes = await NuevoServicio().ListarPendientesAsync();

        // Verificar
        var unica = Assert.Single(pendientes);
        Assert.Equal(pendienteId, unica.Id);
    }

    // RF-GP-02
    [Fact]
    public async Task ListarDelSolicitante_ConSolicitudesDeOtros_DevuelveSoloLasPropias()
    {
        // Preparar
        var otroEstandarId = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
        var propiaId = await CrearSolicitudPendienteAsync(_estandarId);
        await CrearSolicitudPendienteAsync(otroEstandarId);

        // Actuar
        var propias = await NuevoServicio().ListarDelSolicitanteAsync(_estandarId);

        // Verificar
        var unica = Assert.Single(propias);
        Assert.Equal(propiaId, unica.Id);
        Assert.Equal(_estandarId, unica.SolicitanteId);
    }

    [Fact]
    public async Task Aprobar_SolicitudInexistente_LanzaSolicitudNoEncontrada()
    {
        // Preparar
        var idInexistente = Guid.NewGuid();

        // Actuar
        var accion = () => NuevoServicio().AprobarAsync(idInexistente, _administradorId);

        // Verificar
        await Assert.ThrowsAsync<SolicitudNoEncontradaException>(accion);
    }
}
