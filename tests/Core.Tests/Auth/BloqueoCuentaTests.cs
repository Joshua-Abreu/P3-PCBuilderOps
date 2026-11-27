using Core.Auditoria.Services;
using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Auth.Services;
using Core.Notifications.Services;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Core.Tests.Auth;

// RF-CA-19
public sealed class BloqueoCuentaTests : IDisposable
{
    private const string Correo = "bloqueo@prueba.com";
    private const string PasswordCorrecta = "Clave12345";
    private const string PasswordIncorrecta = "Mala12345";

    private sealed class HashSimple : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verificar(string password, string hash) => hash == Hash(password);
    }

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly IConfiguration _configuracion = new ConfigurationBuilder().Build();
    private readonly Guid _usuarioId = Guid.NewGuid();

    public BloqueoCuentaTests()
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        contexto.Usuarios.Add(new Usuario
        {
            Id = _usuarioId,
            Nombre = "Usuario Bloqueo",
            Correo = Correo,
            PasswordHash = new HashSimple().Hash(PasswordCorrecta),
            Rol = RolUsuario.Estandar,
            EstaActivo = true,
            FechaCreacionUtc = DateTime.UtcNow
        });
        contexto.SaveChanges();
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private AuthService NuevoAuthService()
    {
        var contexto = _baseDeDatos.NuevoContexto();
        return new AuthService(
            contexto,
            new HashSimple(),
            new InputValidator(),
            new JwtService(new ConfiguracionJwt("clave-de-pruebas-con-mas-de-32-bytes-1234567", "Pruebas", "Pruebas", 60)),
            _configuracion,
            new EncoladorCorreos(contexto, TimeProvider.System));
    }

    private PasswordService NuevoPasswordService()
    {
        var contexto = _baseDeDatos.NuevoContexto();
        return new PasswordService(
            contexto,
            new HashSimple(),
            new InputValidator(),
            _configuracion,
            new EncoladorCorreos(contexto, TimeProvider.System),
            new Auditor(contexto, TimeProvider.System));
    }

    private Task<LoginResultado> IniciarSesionAsync(string password) =>
        NuevoAuthService().LoginAsync(Correo, password);

    private async Task FallarAsync(int veces)
    {
        for (var i = 0; i < veces; i++)
            await Assert.ThrowsAsync<AutenticacionException>(() => IniciarSesionAsync(PasswordIncorrecta));
    }

    private Usuario LeerUsuario()
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return contexto.Usuarios.AsNoTracking().Single(u => u.Id == _usuarioId);
    }

    [Fact]
    public async Task Login_CincoFallidos_ElSextoConLaCorrectaSeRechazaIndicandoLosMinutos()
    {
        // Preparar
        await FallarAsync(5);

        // Actuar
        var error = await Assert.ThrowsAsync<AutenticacionException>(() => IniciarSesionAsync(PasswordCorrecta));

        // Verificar
        Assert.Equal("La cuenta está bloqueada temporalmente. Intenta de nuevo en 15 minutos.", error.Message);
    }

    [Fact]
    public async Task Login_CuatroFallidos_ElQuintoSigueSiendoCredencialesIncorrectas()
    {
        // Preparar
        await FallarAsync(4);

        // Actuar
        var error = await Assert.ThrowsAsync<AutenticacionException>(() => IniciarSesionAsync(PasswordIncorrecta));

        // Verificar
        Assert.Equal("Credenciales incorrectas.", error.Message);
        Assert.NotNull(LeerUsuario().BloqueadoHastaUtc);
    }

    [Fact]
    public async Task Login_CorrectoAntesDelBloqueo_PoneElContadorEnCero()
    {
        // Preparar
        await FallarAsync(4);
        await IniciarSesionAsync(PasswordCorrecta);
        await FallarAsync(4);

        // Actuar
        var resultado = await IniciarSesionAsync(PasswordCorrecta);

        // Verificar
        Assert.False(string.IsNullOrEmpty(resultado.Token));
    }

    [Fact]
    public async Task Login_BloqueoConMenosDeUnMinuto_IndicaUnMinutoEnSingular()
    {
        // Preparar
        using (var contexto = _baseDeDatos.NuevoContexto())
        {
            var usuario = contexto.Usuarios.Single(u => u.Id == _usuarioId);
            usuario.BloqueadoHastaUtc = DateTime.UtcNow.AddSeconds(30);
            contexto.SaveChanges();
        }

        // Actuar
        var error = await Assert.ThrowsAsync<AutenticacionException>(() => IniciarSesionAsync(PasswordCorrecta));

        // Verificar
        Assert.Equal("La cuenta está bloqueada temporalmente. Intenta de nuevo en 1 minuto.", error.Message);
    }

    // RF-CA-11
    [Fact]
    public async Task Restablecer_ConCodigoValido_QuitaElBloqueoYPermiteEntrarConLaNueva()
    {
        // Preparar
        await FallarAsync(5);
        using (var contexto = _baseDeDatos.NuevoContexto())
        {
            contexto.TokensRecuperacion.Add(new TokenRecuperacion
            {
                Id = Guid.NewGuid(),
                UsuarioId = _usuarioId,
                Token = "codigo-de-prueba",
                FechaCreacionUtc = DateTime.UtcNow,
                ExpiracionUtc = DateTime.UtcNow.AddHours(2)
            });
            contexto.SaveChanges();
        }

        // Actuar
        await NuevoPasswordService().RestablecerPasswordAsync("codigo-de-prueba", "Nueva12345");

        // Verificar
        Assert.Null(LeerUsuario().BloqueadoHastaUtc);
        var resultado = await IniciarSesionAsync("Nueva12345");
        Assert.False(string.IsNullOrEmpty(resultado.Token));
    }

    // RF-CA-22
    [Fact]
    public async Task CambiarPassword_ConCuentaBloqueada_QuitaElBloqueo()
    {
        // Preparar
        await FallarAsync(5);

        // Actuar
        await NuevoPasswordService().CambiarPasswordAsync(_usuarioId, PasswordCorrecta, "Nueva12345");

        // Verificar
        Assert.Null(LeerUsuario().BloqueadoHastaUtc);
        var resultado = await IniciarSesionAsync("Nueva12345");
        Assert.False(string.IsNullOrEmpty(resultado.Token));
    }
}
