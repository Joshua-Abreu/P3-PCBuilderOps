using Core.Auditoria.Domain;
using Core.Auditoria.Services;
using Core.Auth.Domain;
using Core.Data;
using Core.Tests.Soporte;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Data;

// RF-AUD-01, RD-03 · criterios CA-HIS-01, CA-HIS-02 y CA-HIS-05
public sealed class OperacionCompartidaTests : IDisposable
{
    private static readonly DateTime Momento = new(2026, 11, 28, 10, 0, 0, DateTimeKind.Utc);

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly Guid _administradorId;

    public OperacionCompartidaTests()
    {
        _administradorId = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private static Usuario UsuarioNuevo() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Creado por otro contexto",
        Correo = $"{Guid.NewGuid():N}@prueba.com",
        PasswordHash = "hash",
        Rol = RolUsuario.Estandar,
        EstaActivo = true,
        FechaCreacionUtc = Momento
    };

    private void Auditar(AppDbContext contextoCore, Guid entidadId) =>
        new Auditor(contextoCore, new RelojFijo(Momento)).Registrar(
            _administradorId, AccionAuditoria.UsuarioReactivado, EntidadAuditada.Usuario, entidadId.ToString(),
            new { estaActivo = false }, new { estaActivo = true });

    private (bool UsuarioGuardado, bool RegistroGuardado) LeerResultado(Guid usuarioId)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return (
            contexto.Usuarios.Any(u => u.Id == usuarioId),
            contexto.RegistrosAuditoria.Any(r => r.EntidadId == usuarioId.ToString()));
    }

    // CA-HIS-01
    [Fact]
    public async Task EjecutarAsync_CambioYRegistroDelCore_SeGuardanJuntos()
    {
        // Preparar
        using var contextoCore = _baseDeDatos.NuevoContexto();
        using var otroContexto = _baseDeDatos.NuevoContexto();
        var usuario = UsuarioNuevo();

        // Actuar
        await new OperacionCompartida(contextoCore).EjecutarAsync(otroContexto, async ct =>
        {
            otroContexto.Usuarios.Add(usuario);
            await otroContexto.SaveChangesAsync(ct);
            Auditar(contextoCore, usuario.Id);
            return true;
        });

        // Verificar
        Assert.Equal((true, true), LeerResultado(usuario.Id));
    }

    // CA-HIS-02
    [Fact]
    public async Task EjecutarAsync_FallaElGuardadoDelCore_SeDeshaceTambienLoQueGuardoElOtroContexto()
    {
        // Preparar
        using var contextoCore = _baseDeDatos.NuevoContexto(
            new AccionAntesDeGuardar(() => throw new InvalidOperationException("Falla simulada del Core.")));
        using var otroContexto = _baseDeDatos.NuevoContexto();
        var usuario = UsuarioNuevo();

        // Actuar
        var accion = () => new OperacionCompartida(contextoCore).EjecutarAsync(otroContexto, async ct =>
        {
            otroContexto.Usuarios.Add(usuario);
            await otroContexto.SaveChangesAsync(ct);
            Auditar(contextoCore, usuario.Id);
            return true;
        });

        // Verificar
        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        Assert.Equal((false, false), LeerResultado(usuario.Id));
    }

    // CA-HIS-02
    [Fact]
    public async Task EjecutarAsync_ElCambioFallaDespuesDeGuardar_NoQuedaNada()
    {
        // Preparar
        using var contextoCore = _baseDeDatos.NuevoContexto();
        using var otroContexto = _baseDeDatos.NuevoContexto();
        var usuario = UsuarioNuevo();

        // Actuar
        var accion = () => new OperacionCompartida(contextoCore).EjecutarAsync<bool>(otroContexto, async ct =>
        {
            otroContexto.Usuarios.Add(usuario);
            await otroContexto.SaveChangesAsync(ct);
            Auditar(contextoCore, usuario.Id);
            throw new InvalidOperationException("Falla simulada del cambio.");
        });

        // Verificar
        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        Assert.Equal((false, false), LeerResultado(usuario.Id));
    }

    // CA-HIS-05
    [Fact]
    public async Task EjecutarAsync_ContextoConOtraConexion_SeRechazaSinEjecutarElCambio()
    {
        // Preparar
        using var otraConexion = new SqliteConnection("DataSource=:memory:");
        otraConexion.Open();
        using var contextoAjeno = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(otraConexion).Options);
        contextoAjeno.Database.EnsureCreated();
        using var contextoCore = _baseDeDatos.NuevoContexto();
        var seEjecuto = false;

        // Actuar
        var accion = () => new OperacionCompartida(contextoCore).EjecutarAsync(contextoAjeno, _ =>
        {
            seEjecuto = true;
            return Task.FromResult(true);
        });

        // Verificar
        await Assert.ThrowsAsync<ConexionNoCompartidaException>(accion);
        Assert.False(seEjecuto);
    }

    // CA-HIS-05
    [Fact]
    public void LaOperacion_RecibeCualquierDbContextYNoUnTipoDelModulo()
    {
        // Actuar
        var parametro = typeof(IOperacionCompartida).GetMethod(nameof(IOperacionCompartida.EjecutarAsync))!.GetParameters()[0];

        // Verificar
        Assert.Equal(typeof(DbContext), parametro.ParameterType);
        Assert.DoesNotContain(typeof(OperacionCompartida).Assembly.GetReferencedAssemblies(), a => a.Name == "PCBuilderOps");
    }
}
