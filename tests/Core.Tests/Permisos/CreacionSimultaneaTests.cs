using Core.Auth.Domain;
using Core.Data;
using Core.Notifications.Services;
using Core.Permisos.Domain;
using Core.Permisos.Exceptions;
using Core.Permisos.Services;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Permisos;

// Hallazgo 2 (Asignación 3) · RF-NOT-01, RF-GP-01: dos solicitudes simultáneas del mismo usuario.
public sealed class CreacionSimultaneaTests : IDisposable
{
    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly RelojFijo _reloj = new(new DateTime(2026, 11, 26, 9, 0, 0, DateTimeKind.Utc));
    private readonly Guid _solicitanteId;
    private readonly Guid _administrador1;
    private readonly Guid _administrador2;

    public CreacionSimultaneaTests()
    {
        _solicitanteId = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
        _administrador1 = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
        _administrador2 = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private GestionPermisosService ServicioSobre(AppDbContext contexto) =>
        Servicios.GestionPermisos(contexto, _reloj);

    private async Task CrearEnOtraPeticionAsync()
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        await ServicioSobre(contexto).CrearAsync(_solicitanteId, TipoPermiso.OperarTaller);
    }

    private int Contar(Func<AppDbContext, int> consulta)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return consulta(contexto);
    }

    // Hallazgo 2
    [Fact]
    public async Task DosSolicitudesALaVez_SoloSeCreaUnaYCadaAdministradorRecibeUnSoloAviso()
    {
        // Preparar
        using var contexto = _baseDeDatos.NuevoContexto(new AccionAntesDeGuardar(CrearEnOtraPeticionAsync));

        // Actuar
        var segunda = () => ServicioSobre(contexto).CrearAsync(_solicitanteId, TipoPermiso.OperarTaller);

        // Verificar
        var error = await Assert.ThrowsAsync<SolicitudDuplicadaException>(segunda);
        Assert.Equal("Ya tienes una solicitud pendiente para este permiso.", error.Message);
        Assert.Equal(1, Contar(c => c.SolicitudesPermiso.Count(s => s.SolicitanteId == _solicitanteId && s.Estado == EstadoSolicitud.Pendiente)));
        Assert.Equal(1, Contar(c => c.Notificaciones.Count(n => n.UsuarioId == _administrador1)));
        Assert.Equal(1, Contar(c => c.Notificaciones.Count(n => n.UsuarioId == _administrador2)));
        Assert.Equal(2, Contar(c => c.CorreosEnCola.Count()));
    }

    // Hallazgo 2
    [Fact]
    public async Task UnaSolicitudResuelta_NoImpideVolverASolicitarElMismoPermiso()
    {
        // Preparar
        using (var contexto = _baseDeDatos.NuevoContexto())
        {
            var primera = await ServicioSobre(contexto).CrearAsync(_solicitanteId, TipoPermiso.OperarTaller);
            await ServicioSobre(contexto).RechazarAsync(primera.Id, _administrador1, "Sin cupo");
        }

        // Actuar
        using var otro = _baseDeDatos.NuevoContexto();
        var segunda = await ServicioSobre(otro).CrearAsync(_solicitanteId, TipoPermiso.OperarTaller);

        // Verificar
        Assert.Equal(nameof(EstadoSolicitud.Pendiente), segunda.Estado);
        Assert.Equal(2, Contar(c => c.SolicitudesPermiso.Count(s => s.SolicitanteId == _solicitanteId)));
    }
}
