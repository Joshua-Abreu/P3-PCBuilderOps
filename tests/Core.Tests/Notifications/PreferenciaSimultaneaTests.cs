using Core.Auth.Domain;
using Core.Notifications.Services;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Notifications;

// Hallazgo 2 (Asignación 3) · RF-NOT-07: la misma regla de unicidad en la preferencia de correo.
public sealed class PreferenciaSimultaneaTests : IDisposable
{
    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly RelojFijo _reloj = new(new DateTime(2026, 11, 26, 9, 0, 0, DateTimeKind.Utc));
    private readonly Guid _usuarioId;

    public PreferenciaSimultaneaTests() => _usuarioId = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);

    public void Dispose() => _baseDeDatos.Dispose();

    private async Task ActivarCorreoEnOtraPeticionAsync()
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        await new NotificacionesService(contexto, _reloj).ActualizarPreferenciasAsync(_usuarioId, correoActivo: true);
    }

    // Hallazgo 2
    [Fact]
    public async Task PrimeraPreferenciaGuardadaDosVecesALaVez_NoFallaYQuedaLaUltima()
    {
        // Preparar
        using var contexto = _baseDeDatos.NuevoContexto(new AccionAntesDeGuardar(ActivarCorreoEnOtraPeticionAsync));

        // Actuar
        var resultado = await new NotificacionesService(contexto, _reloj).ActualizarPreferenciasAsync(_usuarioId, correoActivo: false);

        // Verificar
        Assert.False(resultado.CorreoActivo);
        using var lectura = _baseDeDatos.NuevoContexto();
        var guardada = await lectura.PreferenciasNotificacion.AsNoTracking().SingleAsync(p => p.UsuarioId == _usuarioId);
        Assert.False(guardada.CorreoActivo);
    }
}
