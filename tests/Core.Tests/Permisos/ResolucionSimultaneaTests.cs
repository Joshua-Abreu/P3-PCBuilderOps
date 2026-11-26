using Core.Auth.Domain;
using Core.Data;
using Core.Notifications.Services;
using Core.Permisos.Domain;
using Core.Permisos.Exceptions;
using Core.Permisos.Services;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Permisos;

// Hallazgo 1 (Asignación 3) · RF-NOT-02, RF-GP-06: dos Administradores resuelven la misma solicitud a la vez.
public sealed class ResolucionSimultaneaTests : IDisposable
{
    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly RelojFijo _reloj = new(new DateTime(2026, 11, 26, 9, 0, 0, DateTimeKind.Utc));
    private readonly Guid _solicitanteId;
    private readonly Guid _administrador1;
    private readonly Guid _administrador2;

    public ResolucionSimultaneaTests()
    {
        _solicitanteId = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
        _administrador1 = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
        _administrador2 = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private GestionPermisosService ServicioSobre(AppDbContext contexto) =>
        new(contexto, _reloj, new Notificador(contexto, new EncoladorCorreos(contexto, _reloj), _reloj));

    private async Task<Guid> CrearSolicitudAsync()
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return (await ServicioSobre(contexto).CrearAsync(_solicitanteId, TipoPermiso.OperarTaller)).Id;
    }

    private async Task<AppDbContext> ContextoQueYaLeyoLaSolicitudAsync(Guid solicitudId)
    {
        var contexto = _baseDeDatos.NuevoContexto();
        await contexto.SolicitudesPermiso.SingleAsync(s => s.Id == solicitudId);
        return contexto;
    }

    private (SolicitudPermiso Solicitud, int Permisos, List<string> Avisos) LeerResultado(Guid solicitudId)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return (
            contexto.SolicitudesPermiso.AsNoTracking().Single(s => s.Id == solicitudId),
            contexto.PermisosUsuario.Count(p => p.UsuarioId == _solicitanteId),
            contexto.Notificaciones.Where(n => n.UsuarioId == _solicitanteId).Select(n => n.Mensaje).ToList());
    }

    // Hallazgo 1
    [Fact]
    public async Task AprobarYRechazarALaVez_SoloGanaLaPrimeraYElSolicitanteRecibeUnSoloAviso()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync();
        using var contextoDelSegundo = await ContextoQueYaLeyoLaSolicitudAsync(solicitudId);

        // Actuar
        using (var contextoDelPrimero = _baseDeDatos.NuevoContexto())
            await ServicioSobre(contextoDelPrimero).AprobarAsync(solicitudId, _administrador1);
        var rechazoTardio = () => ServicioSobre(contextoDelSegundo).RechazarAsync(solicitudId, _administrador2, "Sin cupo");

        // Verificar
        await Assert.ThrowsAsync<TransicionNoPermitidaException>(rechazoTardio);
        var (solicitud, permisos, avisos) = LeerResultado(solicitudId);
        Assert.Equal(EstadoSolicitud.Aplicada, solicitud.Estado);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.Equal(1, permisos);
        Assert.Contains("aprobada", Assert.Single(avisos));
    }

    // Hallazgo 1
    [Fact]
    public async Task RechazarDosVecesALaVez_ConservaElPrimerMotivoYUnSoloAviso()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync();
        using var contextoDelSegundo = await ContextoQueYaLeyoLaSolicitudAsync(solicitudId);

        // Actuar
        using (var contextoDelPrimero = _baseDeDatos.NuevoContexto())
            await ServicioSobre(contextoDelPrimero).RechazarAsync(solicitudId, _administrador1, "Motivo del primero");
        var rechazoTardio = () => ServicioSobre(contextoDelSegundo).RechazarAsync(solicitudId, _administrador2, "Motivo del segundo");

        // Verificar
        await Assert.ThrowsAsync<TransicionNoPermitidaException>(rechazoTardio);
        var (solicitud, permisos, avisos) = LeerResultado(solicitudId);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal("Motivo del primero", solicitud.MotivoRechazo);
        Assert.Equal(_administrador1, solicitud.AprobadorId);
        Assert.Equal(0, permisos);
        Assert.Contains("Motivo del primero", Assert.Single(avisos));
    }

    // Hallazgo 1
    [Fact]
    public async Task AprobarDosVecesALaVez_LaSegundaSeRechazaComoTransicionYNoComoErrorInterno()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync();
        using var contextoDelSegundo = await ContextoQueYaLeyoLaSolicitudAsync(solicitudId);

        // Actuar
        using (var contextoDelPrimero = _baseDeDatos.NuevoContexto())
            await ServicioSobre(contextoDelPrimero).AprobarAsync(solicitudId, _administrador1);
        var aprobacionTardia = () => ServicioSobre(contextoDelSegundo).AprobarAsync(solicitudId, _administrador2);

        // Verificar
        await Assert.ThrowsAsync<TransicionNoPermitidaException>(aprobacionTardia);
        var (solicitud, permisos, avisos) = LeerResultado(solicitudId);
        Assert.Equal(_administrador1, solicitud.AprobadorId);
        Assert.Equal(1, permisos);
        Assert.Single(avisos);
    }
}
