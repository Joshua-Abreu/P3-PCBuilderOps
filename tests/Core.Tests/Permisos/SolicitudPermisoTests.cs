using Core.Auth.Exceptions;
using Core.Permisos.Domain;
using Core.Permisos.Exceptions;

namespace Core.Tests.Permisos;

public class SolicitudPermisoTests
{
    private static readonly Guid SolicitanteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdministradorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime FechaCreacion = new(2026, 10, 18, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaResolucion = new(2026, 10, 18, 15, 30, 0, DateTimeKind.Utc);

    private static SolicitudPermiso NuevaSolicitudPendiente() =>
        SolicitudPermiso.Crear(SolicitanteId, TipoPermiso.OperarTaller, FechaCreacion);

    private static SolicitudPermiso SolicitudAplicada()
    {
        var solicitud = NuevaSolicitudPendiente();
        solicitud.Aprobar(AdministradorId, FechaResolucion);
        solicitud.Aplicar();
        return solicitud;
    }

    private static SolicitudPermiso SolicitudRechazada()
    {
        var solicitud = NuevaSolicitudPendiente();
        solicitud.Rechazar(AdministradorId, "No trabaja en el taller.", FechaResolucion);
        return solicitud;
    }

    // RF-GP-01
    [Fact]
    public void Crear_NuevaSolicitud_NaceEnPendienteSinResolucion()
    {
        // Preparar
        var permisoSolicitado = TipoPermiso.OperarTaller;

        // Actuar
        var solicitud = SolicitudPermiso.Crear(SolicitanteId, permisoSolicitado, FechaCreacion);

        // Verificar
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Equal(SolicitanteId, solicitud.SolicitanteId);
        Assert.Equal(TipoPermiso.OperarTaller, solicitud.Permiso);
        Assert.Equal(FechaCreacion, solicitud.FechaCreacionUtc);
        Assert.Null(solicitud.AprobadorId);
        Assert.Null(solicitud.FechaResolucionUtc);
        Assert.Null(solicitud.MotivoRechazo);
    }

    // RF-GP-03
    [Fact]
    public void Aprobar_SolicitudPendiente_QuedaAprobadaYRegistraAprobadorYFecha()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        solicitud.Aprobar(AdministradorId, FechaResolucion);

        // Verificar
        Assert.Equal(EstadoSolicitud.Aprobada, solicitud.Estado);
        Assert.Equal(AdministradorId, solicitud.AprobadorId);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucionUtc);
        Assert.Null(solicitud.MotivoRechazo);
    }

    // RF-GP-05
    [Fact]
    public void Aplicar_SolicitudAprobada_QuedaAplicadaConservandoAprobadorYFecha()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();
        solicitud.Aprobar(AdministradorId, FechaResolucion);

        // Actuar
        solicitud.Aplicar();

        // Verificar
        Assert.Equal(EstadoSolicitud.Aplicada, solicitud.Estado);
        Assert.Equal(AdministradorId, solicitud.AprobadorId);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucionUtc);
    }

    // RF-GP-04
    [Fact]
    public void Rechazar_SolicitudPendienteConMotivo_QuedaRechazadaYRegistraMotivoAprobadorYFecha()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        solicitud.Rechazar(AdministradorId, "  No trabaja en el taller.  ", FechaResolucion);

        // Verificar
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal("No trabaja en el taller.", solicitud.MotivoRechazo);
        Assert.Equal(AdministradorId, solicitud.AprobadorId);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucionUtc);
    }

    // RF-GP-04
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechazar_SinMotivo_SeRechazaYLaSolicitudSigueIntacta(string? motivo)
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        var accion = () => solicitud.Rechazar(AdministradorId, motivo, FechaResolucion);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Null(solicitud.AprobadorId);
        Assert.Null(solicitud.FechaResolucionUtc);
        Assert.Null(solicitud.MotivoRechazo);
    }

    // RF-GP-04
    [Fact]
    public void Rechazar_MotivoMasLargoQueElMaximo_SeRechazaYLaSolicitudSigueEnPendiente()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();
        var motivoLargo = new string('x', SolicitudPermiso.MotivoRechazoMaximo + 1);

        // Actuar
        var accion = () => solicitud.Rechazar(AdministradorId, motivoLargo, FechaResolucion);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
    }

    // RF-GP-06
    [Fact]
    public void Aprobar_SolicitudYaAplicada_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var solicitud = SolicitudAplicada();
        var otroAdministrador = Guid.NewGuid();
        var otraFecha = FechaResolucion.AddHours(1);

        // Actuar
        var accion = () => solicitud.Aprobar(otroAdministrador, otraFecha);

        // Verificar
        Assert.Throws<TransicionNoPermitidaException>(accion);
        Assert.Equal(EstadoSolicitud.Aplicada, solicitud.Estado);
        Assert.Equal(AdministradorId, solicitud.AprobadorId);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucionUtc);
    }

    // RF-GP-06
    [Fact]
    public void Aplicar_SolicitudRechazada_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var solicitud = SolicitudRechazada();

        // Actuar
        var accion = () => solicitud.Aplicar();

        // Verificar
        Assert.Throws<TransicionNoPermitidaException>(accion);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal("No trabaja en el taller.", solicitud.MotivoRechazo);
    }

    // RF-GP-06
    [Fact]
    public void Aplicar_SolicitudPendienteSinAprobar_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        var accion = () => solicitud.Aplicar();

        // Verificar
        Assert.Throws<TransicionNoPermitidaException>(accion);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
    }

    // RF-GP-06
    [Fact]
    public void Aprobar_SolicitudRechazada_SeRechazaYConservaElRechazo()
    {
        // Preparar
        var solicitud = SolicitudRechazada();

        // Actuar
        var accion = () => solicitud.Aprobar(AdministradorId, FechaResolucion.AddHours(1));

        // Verificar
        Assert.Throws<TransicionNoPermitidaException>(accion);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucionUtc);
        Assert.Equal("No trabaja en el taller.", solicitud.MotivoRechazo);
    }

    // RF-GP-06
    [Fact]
    public void Rechazar_SolicitudYaAplicada_SeRechazaYNoRegistraMotivo()
    {
        // Preparar
        var solicitud = SolicitudAplicada();

        // Actuar
        var accion = () => solicitud.Rechazar(AdministradorId, "Cambio de opinión.", FechaResolucion.AddHours(1));

        // Verificar
        Assert.Throws<TransicionNoPermitidaException>(accion);
        Assert.Equal(EstadoSolicitud.Aplicada, solicitud.Estado);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucionUtc);
    }

    // RF-GP-04, RF-GP-06
    [Fact]
    public void Rechazar_SolicitudAprobada_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();
        solicitud.Aprobar(AdministradorId, FechaResolucion);

        // Actuar
        var accion = () => solicitud.Rechazar(AdministradorId, "Cambio de opinión.", FechaResolucion.AddHours(1));

        // Verificar
        Assert.Throws<TransicionNoPermitidaException>(accion);
        Assert.Equal(EstadoSolicitud.Aprobada, solicitud.Estado);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucionUtc);
    }

    // RF-GP-04, RF-GP-06
    [Fact]
    public void Rechazar_SolicitudYaRechazada_SeRechazaYConservaElPrimerRechazo()
    {
        // Preparar
        var solicitud = SolicitudRechazada();

        // Actuar
        var accion = () => solicitud.Rechazar(AdministradorId, "Segundo motivo.", FechaResolucion.AddHours(1));

        // Verificar
        Assert.Throws<TransicionNoPermitidaException>(accion);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal("No trabaja en el taller.", solicitud.MotivoRechazo);
        Assert.Equal(FechaResolucion, solicitud.FechaResolucionUtc);
    }

    // RF-GP-07
    [Fact]
    public void Aprobar_PorElPropioSolicitante_SeRechazaYLaSolicitudSigueEnPendiente()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        var accion = () => solicitud.Aprobar(SolicitanteId, FechaResolucion);

        // Verificar
        Assert.Throws<ResolucionPropiaException>(accion);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Null(solicitud.AprobadorId);
    }

    // RF-GP-07
    [Fact]
    public void Rechazar_PorElPropioSolicitante_SeRechazaYLaSolicitudSigueEnPendiente()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        var accion = () => solicitud.Rechazar(SolicitanteId, "Ya no lo necesito.", FechaResolucion);

        // Verificar
        Assert.Throws<ResolucionPropiaException>(accion);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Null(solicitud.MotivoRechazo);
    }
}
