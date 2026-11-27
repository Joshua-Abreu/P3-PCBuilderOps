using System.Text;
using System.Text.Json;
using Core.Auditoria.Domain;
using Core.Auditoria.Services;
using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Auth.Services;
using Core.Data;
using Core.Documentos.Exceptions;
using Core.Documentos.Services;
using Core.Notifications.Services;
using Core.Permisos.Domain;
using Core.Permisos.Exceptions;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Core.Tests.Auditoria;

// RF-AUD-01 a RF-AUD-05 · criterios CA-AUD-01 a CA-AUD-12
public sealed class EventosAuditadosTests : IDisposable
{
    private const string PasswordInicial = "Inicial12345";
    private static readonly DateTime Momento = new(2026, 11, 27, 15, 30, 0, DateTimeKind.Utc);

    private sealed class HashSimple : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verificar(string password, string hash) => hash == Hash(password);
    }

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly RelojFijo _reloj = new(Momento);
    private readonly IConfiguration _configuracion = new ConfigurationBuilder().Build();
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "pcbuilderops-pruebas", Guid.NewGuid().ToString("N"));
    private readonly Guid _administrador;
    private readonly Guid _estandar;
    private readonly Guid _otroEstandar;

    public EventosAuditadosTests()
    {
        _administrador = _baseDeDatos.CrearUsuario(RolUsuario.Administrador, "Ana Administradora");
        _estandar = _baseDeDatos.CrearUsuario(RolUsuario.Estandar, "Elena Pérez");
        _otroEstandar = _baseDeDatos.CrearUsuario(RolUsuario.Estandar, "Esteban Gómez");

        using var contexto = _baseDeDatos.NuevoContexto();
        foreach (var usuario in contexto.Usuarios)
            usuario.PasswordHash = new HashSimple().Hash(PasswordInicial);
        contexto.SaveChanges();
    }

    public void Dispose()
    {
        _baseDeDatos.Dispose();

        if (Directory.Exists(_carpeta))
            Directory.Delete(_carpeta, recursive: true);
    }

    private UsuarioAdminService ServicioUsuarios(AppDbContext contexto) =>
        new(contexto, new HashSimple(), new InputValidator(), _configuracion, new EncoladorCorreos(contexto, _reloj), new Auditor(contexto, _reloj));

    private PasswordService ServicioContrasenas(AppDbContext contexto) =>
        new(contexto, new HashSimple(), new InputValidator(), _configuracion, new EncoladorCorreos(contexto, _reloj), new Auditor(contexto, _reloj));

    private DocumentosService ServicioDocumentos(AppDbContext contexto) =>
        new(contexto, new AlmacenamientoArchivosLocal(_carpeta), _reloj, new Auditor(contexto, _reloj));

    private async Task<T> EnPeticion<T>(Func<AppDbContext, Task<T>> operacion)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return await operacion(contexto);
    }

    private async Task EnPeticion(Func<AppDbContext, Task> operacion)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        await operacion(contexto);
    }

    private List<RegistroAuditoria> Registros(string? accion = null)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return contexto.RegistrosAuditoria.AsNoTracking()
            .Where(r => accion == null || r.Accion == accion)
            .OrderBy(r => r.FechaUtc)
            .ToList();
    }

    private string TokenDeRecuperacion(Guid usuarioId)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return contexto.TokensRecuperacion.Single(t => t.UsuarioId == usuarioId && !t.FueUsado).Token;
    }

    private static ArchivoSubido Archivo(string contenido = "Cinebench R23: 15000") =>
        new("benchmark.txt", "text/plain", Encoding.UTF8.GetByteCount(contenido), new MemoryStream(Encoding.UTF8.GetBytes(contenido)));

    private static JsonElement Json(string texto) => JsonDocument.Parse(texto).RootElement;

    // CA-AUD-01 · RF-AUD-01: los ocho datos, presentes y no vacíos.
    private static void VerificarCampos(RegistroAuditoria registro, Guid usuarioId, string accion, string entidad, Guid entidadId)
    {
        Assert.NotEqual(Guid.Empty, registro.Id);
        Assert.Equal(usuarioId, registro.UsuarioId);
        Assert.Equal(accion, registro.Accion);
        Assert.Equal(entidad, registro.Entidad);
        Assert.Equal(entidadId.ToString(), registro.EntidadId);
        Assert.Equal(Momento, registro.FechaUtc);
        Assert.Equal(DateTimeKind.Utc, registro.FechaUtc.Kind);
        Assert.Equal(JsonValueKind.Object, Json(registro.ValorAnterior).ValueKind);
        Assert.Equal(JsonValueKind.Object, Json(registro.ValorNuevo).ValueKind);
        Assert.NotEmpty(Json(registro.ValorAnterior).EnumerateObject());
        Assert.NotEmpty(Json(registro.ValorNuevo).EnumerateObject());
    }

    // CA-AUD-11 · RF-AUD-05
    private static void VerificarSinSecretos(RegistroAuditoria registro, params string[] secretos)
    {
        var texto = registro.ValorAnterior + registro.ValorNuevo;
        Assert.DoesNotContain("hash", texto, StringComparison.OrdinalIgnoreCase);
        foreach (var secreto in secretos)
            Assert.DoesNotContain(secreto, texto, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Guid> CrearSolicitudAsync() =>
        (await EnPeticion(c => Servicios.GestionPermisos(c, _reloj).CrearAsync(_estandar, TipoPermiso.OperarTaller))).Id;

    // CA-AUD-02 · RF-AUD-02
    [Fact]
    public async Task AprobarSolicitud_RegistraQuienAproboYElEstadoAnteriorYNuevo()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync();

        // Actuar
        await EnPeticion(c => Servicios.GestionPermisos(c, _reloj).AprobarAsync(solicitudId, _administrador));

        // Verificar
        var registro = Assert.Single(Registros());
        VerificarCampos(registro, _administrador, AccionAuditoria.SolicitudAprobada, EntidadAuditada.SolicitudPermiso, solicitudId);
        Assert.Equal("Pendiente", Json(registro.ValorAnterior).GetProperty("estado").GetString());
        Assert.Equal("Aplicada", Json(registro.ValorNuevo).GetProperty("estado").GetString());
        Assert.Equal("OperarTaller", Json(registro.ValorNuevo).GetProperty("permiso").GetString());
        Assert.Equal(_estandar, Json(registro.ValorNuevo).GetProperty("solicitanteId").GetGuid());
    }

    // CA-AUD-03 · RF-AUD-02
    [Fact]
    public async Task RechazarSolicitud_RegistraElMotivo()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync();

        // Actuar
        await EnPeticion(c => Servicios.GestionPermisos(c, _reloj).RechazarAsync(solicitudId, _administrador, "Falta experiencia en taller"));

        // Verificar
        var registro = Assert.Single(Registros());
        VerificarCampos(registro, _administrador, AccionAuditoria.SolicitudRechazada, EntidadAuditada.SolicitudPermiso, solicitudId);
        Assert.Equal("Pendiente", Json(registro.ValorAnterior).GetProperty("estado").GetString());
        Assert.Equal("Rechazada", Json(registro.ValorNuevo).GetProperty("estado").GetString());
        Assert.Equal("Falta experiencia en taller", Json(registro.ValorNuevo).GetProperty("motivo").GetString());
    }

    // CA-AUD-04 · RF-AUD-02
    [Fact]
    public async Task ResolucionRechazada_NoDejaRegistro()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync();
        await EnPeticion(c => Servicios.GestionPermisos(c, _reloj).AprobarAsync(solicitudId, _administrador));
        var otraId = (await EnPeticion(c => Servicios.GestionPermisos(c, _reloj).CrearAsync(_otroEstandar, TipoPermiso.OperarTaller))).Id;

        // Actuar
        await Assert.ThrowsAsync<TransicionNoPermitidaException>(
            () => EnPeticion(c => Servicios.GestionPermisos(c, _reloj).AprobarAsync(solicitudId, _administrador)));
        await Assert.ThrowsAsync<ValidacionException>(
            () => EnPeticion(c => Servicios.GestionPermisos(c, _reloj).RechazarAsync(otraId, _administrador, "  ")));

        // Verificar
        Assert.Single(Registros());
    }

    // CA-AUD-05 · RF-AUD-03, RF-DOC-06
    [Fact]
    public async Task SubirDocumento_RegistraQueNoExistiaYLosDatosDelArchivo()
    {
        // Actuar
        var documento = await EnPeticion(c => ServicioDocumentos(c).SubirAsync(_estandar, Archivo()));
        await Assert.ThrowsAsync<ValidacionException>(() => EnPeticion(c => ServicioDocumentos(c).SubirAsync(_estandar, Archivo(""))));

        // Verificar
        var registro = Assert.Single(Registros());
        VerificarCampos(registro, _estandar, AccionAuditoria.DocumentoSubido, EntidadAuditada.Documento, documento.Id);
        Assert.Equal("NoExistia", Json(registro.ValorAnterior).GetProperty("estado").GetString());
        Assert.Equal("Activo", Json(registro.ValorNuevo).GetProperty("estado").GetString());
        Assert.Equal("benchmark.txt", Json(registro.ValorNuevo).GetProperty("nombre").GetString());
        Assert.Equal("text/plain", Json(registro.ValorNuevo).GetProperty("tipoContenido").GetString());
        Assert.Equal(documento.TamanoBytes, Json(registro.ValorNuevo).GetProperty("tamanoBytes").GetInt64());
    }

    // CA-AUD-06 · RF-AUD-03, RF-DOC-06
    [Fact]
    public async Task EliminarDocumento_RegistraAQuienLoEliminoSeaElDuenoOElAdministrador()
    {
        // Preparar
        var propio = await EnPeticion(c => ServicioDocumentos(c).SubirAsync(_estandar, Archivo()));
        var otro = await EnPeticion(c => ServicioDocumentos(c).SubirAsync(_estandar, Archivo()));

        // Actuar
        await Assert.ThrowsAsync<AccesoDocumentoDenegadoException>(
            () => EnPeticion(c => ServicioDocumentos(c).EliminarAsync(new ActorDocumento(_otroEstandar, false), propio.Id)));
        await EnPeticion(c => ServicioDocumentos(c).EliminarAsync(new ActorDocumento(_estandar, false), propio.Id));
        await EnPeticion(c => ServicioDocumentos(c).EliminarAsync(new ActorDocumento(_administrador, true), otro.Id));

        // Verificar
        var eliminaciones = Registros(AccionAuditoria.DocumentoEliminado);
        Assert.Equal(2, eliminaciones.Count);
        var delDueno = eliminaciones.Single(r => r.EntidadId == propio.Id.ToString());
        var delAdministrador = eliminaciones.Single(r => r.EntidadId == otro.Id.ToString());
        VerificarCampos(delDueno, _estandar, AccionAuditoria.DocumentoEliminado, EntidadAuditada.Documento, propio.Id);
        VerificarCampos(delAdministrador, _administrador, AccionAuditoria.DocumentoEliminado, EntidadAuditada.Documento, otro.Id);
        Assert.Equal("Activo", Json(delDueno.ValorAnterior).GetProperty("estado").GetString());
        Assert.Equal("Eliminado", Json(delDueno.ValorNuevo).GetProperty("estado").GetString());
    }

    // CA-AUD-07 · RF-AUD-04, RF-CA-08
    [Fact]
    public async Task CambiarRol_RegistraElRolAnteriorYElNuevo()
    {
        // Actuar
        await EnPeticion(c => ServicioUsuarios(c).CambiarRolAsync(_estandar, RolUsuario.Administrador, _administrador));
        await EnPeticion(c => ServicioUsuarios(c).CambiarRolAsync(_estandar, RolUsuario.Estandar, _administrador));
        await EnPeticion(c => ServicioUsuarios(c).CambiarRolAsync(_estandar, RolUsuario.Estandar, _administrador));

        // Verificar
        var cambios = Registros(AccionAuditoria.RolCambiado);
        Assert.Equal(2, cambios.Count);
        Assert.All(cambios, r => VerificarCampos(r, _administrador, AccionAuditoria.RolCambiado, EntidadAuditada.Usuario, _estandar));
        Assert.Contains(cambios, r => r.ValorAnterior == """{"rol":"Estandar"}""" && r.ValorNuevo == """{"rol":"Administrador"}""");
        Assert.Contains(cambios, r => r.ValorAnterior == """{"rol":"Administrador"}""" && r.ValorNuevo == """{"rol":"Estandar"}""");
    }

    // CA-AUD-08 · RF-AUD-04, RF-CA-20
    [Fact]
    public async Task DesactivarYReactivar_RegistranElEstadoAnteriorYElNuevo()
    {
        // Actuar
        await EnPeticion(c => ServicioUsuarios(c).CambiarEstadoAsync(_otroEstandar, activar: false, _administrador));
        await EnPeticion(c => ServicioUsuarios(c).CambiarEstadoAsync(_otroEstandar, activar: true, _administrador));
        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => EnPeticion(c => ServicioUsuarios(c).CambiarEstadoAsync(_administrador, activar: false, _administrador)));

        // Verificar
        var desactivado = Assert.Single(Registros(AccionAuditoria.UsuarioDesactivado));
        var reactivado = Assert.Single(Registros(AccionAuditoria.UsuarioReactivado));
        VerificarCampos(desactivado, _administrador, AccionAuditoria.UsuarioDesactivado, EntidadAuditada.Usuario, _otroEstandar);
        VerificarCampos(reactivado, _administrador, AccionAuditoria.UsuarioReactivado, EntidadAuditada.Usuario, _otroEstandar);
        Assert.Equal("""{"estaActivo":true}""", desactivado.ValorAnterior);
        Assert.Equal("""{"estaActivo":false}""", desactivado.ValorNuevo);
        Assert.Equal("""{"estaActivo":false}""", reactivado.ValorAnterior);
        Assert.Equal("""{"estaActivo":true}""", reactivado.ValorNuevo);
        Assert.Equal(2, Registros().Count);
    }

    // CA-AUD-09, CA-AUD-11 · RF-AUD-05, RF-CA-13
    [Fact]
    public async Task RestablecimientoForzado_RegistraAlAdministradorSinContrasenaNiCodigo()
    {
        // Actuar
        await EnPeticion(c => ServicioContrasenas(c).ForzarRestablecimientoAsync(_estandar, _administrador));

        // Verificar
        var registro = Assert.Single(Registros());
        VerificarCampos(registro, _administrador, AccionAuditoria.RestablecimientoForzado, EntidadAuditada.Usuario, _estandar);
        Assert.Equal("""{"debeCambiarPassword":false}""", registro.ValorAnterior);
        Assert.Equal("""{"debeCambiarPassword":true,"iniciadoPor":"Administrador"}""", registro.ValorNuevo);
        VerificarSinSecretos(registro, PasswordInicial, TokenDeRecuperacion(_estandar));
    }

    // CA-AUD-10, CA-AUD-11 · RF-AUD-05
    [Fact]
    public async Task RestablecerConElCodigoPedidoPorElUsuario_RegistraQueLoInicioElUsuario()
    {
        // Preparar
        using (var lectura = _baseDeDatos.NuevoContexto())
        {
            var correo = lectura.Usuarios.Single(u => u.Id == _estandar).Correo;
            await EnPeticion(c => ServicioContrasenas(c).OlvidePasswordAsync(correo));
        }
        var codigo = TokenDeRecuperacion(_estandar);

        // Actuar
        await EnPeticion(c => ServicioContrasenas(c).RestablecerPasswordAsync(codigo, "Nueva12345"));

        // Verificar
        var registro = Assert.Single(Registros());
        VerificarCampos(registro, _estandar, AccionAuditoria.ContrasenaRestablecida, EntidadAuditada.Usuario, _estandar);
        Assert.Equal(IniciadoPor.Usuario, Json(registro.ValorNuevo).GetProperty("iniciadoPor").GetString());
        VerificarSinSecretos(registro, PasswordInicial, "Nueva12345", codigo);
    }

    // CA-AUD-10, CA-AUD-11 · RF-AUD-05, RF-CA-13
    [Fact]
    public async Task RestablecerConElCodigoDeUnRestablecimientoForzado_RegistraQueLoInicioUnAdministrador()
    {
        // Preparar
        await EnPeticion(c => ServicioContrasenas(c).ForzarRestablecimientoAsync(_estandar, _administrador));
        var codigo = TokenDeRecuperacion(_estandar);

        // Actuar
        await EnPeticion(c => ServicioContrasenas(c).RestablecerPasswordAsync(codigo, "Nueva12345"));
        await Assert.ThrowsAnyAsync<Exception>(() => EnPeticion(c => ServicioContrasenas(c).RestablecerPasswordAsync(codigo, "Otra12345")));

        // Verificar
        var registro = Assert.Single(Registros(AccionAuditoria.ContrasenaRestablecida));
        VerificarCampos(registro, _estandar, AccionAuditoria.ContrasenaRestablecida, EntidadAuditada.Usuario, _estandar);
        Assert.Equal("""{"debeCambiarPassword":true}""", registro.ValorAnterior);
        Assert.Equal(IniciadoPor.Administrador, Json(registro.ValorNuevo).GetProperty("iniciadoPor").GetString());
        VerificarSinSecretos(registro, "Nueva12345", "Otra12345", codigo);
        Assert.Equal(2, Registros().Count);
    }

    // CA-AUD-12, CA-AUD-11 · RF-AUD-05
    [Fact]
    public async Task CambiarLaPropiaContrasena_RegistraAlUsuarioSinLaContrasena()
    {
        // Actuar
        await Assert.ThrowsAsync<ValidacionException>(
            () => EnPeticion(c => ServicioContrasenas(c).CambiarPasswordAsync(_estandar, "Equivocada12345", "Nueva12345")));
        await EnPeticion(c => ServicioContrasenas(c).CambiarPasswordAsync(_estandar, PasswordInicial, "Nueva12345"));

        // Verificar
        var registro = Assert.Single(Registros());
        VerificarCampos(registro, _estandar, AccionAuditoria.ContrasenaCambiada, EntidadAuditada.Usuario, _estandar);
        Assert.Equal("""{"debeCambiarPassword":false,"iniciadoPor":"Usuario"}""", registro.ValorNuevo);
        VerificarSinSecretos(registro, PasswordInicial, "Nueva12345", "Equivocada12345");
    }
}
