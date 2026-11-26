using System.Text.Json;
using Core.Auth.Domain;
using Core.Notifications.Services;
using Core.Permisos.Domain;
using Core.Permisos.Services;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Data;

// Hallazgo 4 (Asignación 3) · RD-11: todas las fechas del Core se leen de la base como UTC.
public sealed class FechasUtcTests : IDisposable
{
    private static readonly DateTime Momento = new(2026, 11, 26, 22, 11, 15, DateTimeKind.Utc);

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly RelojFijo _reloj = new(Momento);

    public void Dispose() => _baseDeDatos.Dispose();

    // Hallazgo 4
    [Fact]
    public void TodaPropiedadDeFechaDelCore_SeLeeDeLaBaseComoUtc()
    {
        // Preparar
        using var contexto = _baseDeDatos.NuevoContexto();
        var propiedades = contexto.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
            .ToList();
        var leidaSinZona = new DateTime(2026, 11, 26, 22, 11, 15, DateTimeKind.Unspecified);

        // Actuar
        var sinUtc = propiedades
            .Where(p => p.GetValueConverter()?.ConvertFromProvider(leidaSinZona) is not DateTime { Kind: DateTimeKind.Utc })
            .Select(p => $"{p.DeclaringType.ClrType.Name}.{p.Name}")
            .ToList();

        // Verificar
        Assert.NotEmpty(propiedades);
        Assert.Empty(sinUtc);
    }

    // Hallazgo 4
    [Fact]
    public async Task SolicitudCorreoYNotificacion_LeidasDeLaBase_SalenConZ()
    {
        // Preparar
        var solicitanteId = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
        var administradorId = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
        using (var contexto = _baseDeDatos.NuevoContexto())
            await new GestionPermisosService(contexto, _reloj, new Notificador(contexto, new EncoladorCorreos(contexto, _reloj), _reloj))
                .CrearAsync(solicitanteId, TipoPermiso.OperarTaller);

        // Actuar
        using var lectura = _baseDeDatos.NuevoContexto();
        var solicitudes = await new GestionPermisosService(lectura, _reloj, new Notificador(lectura, new EncoladorCorreos(lectura, _reloj), _reloj))
            .ListarDelSolicitanteAsync(solicitanteId);
        var correo = await lectura.CorreosEnCola.AsNoTracking().SingleAsync();
        var usuario = await lectura.Usuarios.AsNoTracking().SingleAsync(u => u.Id == solicitanteId);
        var bandeja = await new NotificacionesService(lectura, _reloj).ListarAsync(administradorId, soloNoLeidas: false);

        // Verificar
        Assert.Equal("\"2026-11-26T22:11:15Z\"", JsonSerializer.Serialize(Assert.Single(solicitudes).FechaCreacionUtc));
        Assert.Equal(DateTimeKind.Utc, correo.FechaCreacionUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, usuario.FechaCreacionUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, Assert.Single(bandeja.Notificaciones).FechaCreacionUtc.Kind);
    }
}
