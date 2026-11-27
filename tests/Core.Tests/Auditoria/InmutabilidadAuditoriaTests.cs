using System.Reflection;
using Core.Auditoria.Domain;
using Core.Auditoria.Exceptions;
using Core.Auditoria.Services;
using Core.Auth.Domain;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Auditoria;

// RF-AUD-06 · criterio CA-AUD-13
public sealed class InmutabilidadAuditoriaTests : IDisposable
{
    private static readonly DateTime Momento = new(2026, 11, 27, 15, 30, 0, DateTimeKind.Utc);

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly Guid _registroId;

    public InmutabilidadAuditoriaTests()
    {
        var administrador = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);

        using var contexto = _baseDeDatos.NuevoContexto();
        var registro = RegistroAuditoria.Crear(
            administrador, AccionAuditoria.RolCambiado, EntidadAuditada.Usuario, Guid.NewGuid().ToString(),
            """{"rol":"Estandar"}""", """{"rol":"Administrador"}""", Momento);
        contexto.RegistrosAuditoria.Add(registro);
        contexto.SaveChanges();
        _registroId = registro.Id;
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private RegistroAuditoria LeerGuardado()
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return contexto.RegistrosAuditoria.AsNoTracking().Single(r => r.Id == _registroId);
    }

    // CA-AUD-13
    [Fact]
    public void LasInterfacesPublicas_SoloRegistranYConsultan()
    {
        // Preparar
        var operaciones = typeof(IAuditor).GetMethods().Concat(typeof(IHistorialAuditoria).GetMethods()).Select(m => m.Name);

        // Actuar
        var nombres = operaciones.Order().ToList();

        // Verificar
        Assert.Equal(new[] { "ConsultarAsync", "Registrar" }, nombres);
    }

    // CA-AUD-13
    [Fact]
    public void ElRegistro_NoTienePropiedadesQueSePuedanCambiarDesdeAfuera()
    {
        // Actuar
        var conSetterPublico = typeof(RegistroAuditoria)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true })
            .Select(p => p.Name)
            .ToList();

        // Verificar
        Assert.Empty(conSetterPublico);
    }

    // CA-AUD-13
    [Fact]
    public async Task ModificarUnRegistroGuardado_SeRechazaYElRegistroNoCambia()
    {
        // Preparar
        using var contexto = _baseDeDatos.NuevoContexto();
        var registro = await contexto.RegistrosAuditoria.SingleAsync(r => r.Id == _registroId);
        contexto.Entry(registro).Property(r => r.ValorNuevo).CurrentValue = """{"rol":"Estandar"}""";

        // Actuar
        var guardar = () => contexto.SaveChangesAsync();

        // Verificar
        await Assert.ThrowsAsync<HistorialInmutableException>(guardar);
        Assert.Equal("""{"rol":"Administrador"}""", LeerGuardado().ValorNuevo);
    }

    // CA-AUD-13
    [Fact]
    public void BorrarUnRegistroGuardado_SeRechazaYElRegistroSigueAhi()
    {
        // Preparar
        using var contexto = _baseDeDatos.NuevoContexto();
        contexto.RegistrosAuditoria.Remove(contexto.RegistrosAuditoria.Single(r => r.Id == _registroId));

        // Actuar
        var guardar = () => { contexto.SaveChanges(); };

        // Verificar
        Assert.Throws<HistorialInmutableException>(guardar);
        Assert.Equal(_registroId, LeerGuardado().Id);
    }
}
