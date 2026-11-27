using Core.Auditoria.Services;
using Core.Auth.Domain;
using Core.Auth.Services;
using Core.Notifications.Domain;
using Core.Notifications.Exceptions;
using Core.Notifications.Services;
using Core.Permisos.Domain;
using Core.Permisos.Exceptions;
using Core.Permisos.Services;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Core.Tests.Notifications;

public sealed class NotificacionesTests : IDisposable
{
    private const string Motivo = "Falta experiencia en taller";
    private static readonly DateTime Inicio = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

    private sealed class HashSimple : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verificar(string password, string hash) => hash == Hash(password);
    }

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly RelojFijo _reloj = new(Inicio);
    private readonly Guid _administrador1;
    private readonly Guid _administrador2;
    private readonly Guid _administradorInactivo;
    private readonly Guid _estandar1;
    private readonly Guid _estandar2;

    public NotificacionesTests()
    {
        _administrador1 = _baseDeDatos.CrearUsuario(RolUsuario.Administrador, "Ana Administradora");
        _administrador2 = _baseDeDatos.CrearUsuario(RolUsuario.Administrador, "Alberto Administrador");
        _administradorInactivo = _baseDeDatos.CrearUsuario(RolUsuario.Administrador, "Inés Inactiva", estaActivo: false);
        _estandar1 = _baseDeDatos.CrearUsuario(RolUsuario.Estandar, "Elena Pérez");
        _estandar2 = _baseDeDatos.CrearUsuario(RolUsuario.Estandar, "Esteban Gómez");
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private GestionPermisosService NuevaGestion()
    {
        var contexto = _baseDeDatos.NuevoContexto();
        return Servicios.GestionPermisos(contexto, _reloj);
    }

    private NotificacionesService NuevaBandeja() => new(_baseDeDatos.NuevoContexto(), _reloj);

    private void Avanzar(int minutos) => _reloj.Fijar(_reloj.GetUtcNow().UtcDateTime.AddMinutes(minutos));

    private async Task<Guid> CrearSolicitudAsync(Guid solicitanteId)
    {
        var solicitud = await NuevaGestion().CrearAsync(solicitanteId, TipoPermiso.OperarTaller);
        Avanzar(5);
        return solicitud.Id;
    }

    private List<Notificacion> NotificacionesDe(Guid usuarioId)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return contexto.Notificaciones.AsNoTracking().Where(n => n.UsuarioId == usuarioId).ToList();
    }

    private int TotalNotificaciones()
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return contexto.Notificaciones.Count();
    }

    private List<CorreoEnCola> CorreosDe(Guid usuarioId)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        var correo = contexto.Usuarios.Single(u => u.Id == usuarioId).Correo;
        return contexto.CorreosEnCola.AsNoTracking().Where(c => c.Destinatario == correo).ToList();
    }

    private void GuardarNotificacion(Guid usuarioId, string asunto, DateTime fechaUtc, bool leida = false)
    {
        var notificacion = Notificacion.Nueva(usuarioId, asunto, $"Contenido de {asunto}", fechaUtc);

        if (leida)
            notificacion.MarcarLeida(fechaUtc.AddMinutes(1));

        using var contexto = _baseDeDatos.NuevoContexto();
        contexto.Notificaciones.Add(notificacion);
        contexto.SaveChanges();
    }

    // CA-NOT-01 · RF-NOT-01
    [Fact]
    public async Task CrearSolicitud_NotificaSoloALosAdministradoresActivos()
    {
        // Actuar
        await CrearSolicitudAsync(_estandar1);

        // Verificar
        foreach (var administrador in new[] { _administrador1, _administrador2 })
        {
            var notificacion = Assert.Single(NotificacionesDe(administrador));
            Assert.False(notificacion.EsLeida);
            Assert.Contains("Elena Pérez", notificacion.Mensaje);
            Assert.Contains(nameof(TipoPermiso.OperarTaller), notificacion.Mensaje);
        }

        Assert.Empty(NotificacionesDe(_estandar1));
        Assert.Empty(NotificacionesDe(_estandar2));
        Assert.Empty(NotificacionesDe(_administradorInactivo));
    }

    // CA-NOT-01 · RF-NOT-01
    [Fact]
    public async Task CrearSolicitud_DeUnAdministrador_NoLoNotificaASiMismo()
    {
        // Actuar
        await CrearSolicitudAsync(_administrador1);

        // Verificar
        Assert.Empty(NotificacionesDe(_administrador1));
        Assert.Single(NotificacionesDe(_administrador2));
    }

    // CA-NOT-02 · RF-NOT-02
    [Fact]
    public async Task Aprobar_NotificaSoloAlSolicitanteQueFueAprobada()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync(_estandar1);
        var antes = TotalNotificaciones();

        // Actuar
        await NuevaGestion().AprobarAsync(solicitudId, _administrador1);

        // Verificar
        var notificacion = Assert.Single(NotificacionesDe(_estandar1));
        Assert.False(notificacion.EsLeida);
        Assert.Contains("aprobada", notificacion.Mensaje);
        Assert.Contains(nameof(TipoPermiso.OperarTaller), notificacion.Mensaje);
        Assert.Equal(antes + 1, TotalNotificaciones());
    }

    // CA-NOT-03 · RF-NOT-02
    [Fact]
    public async Task Rechazar_NotificaAlSolicitanteConElMotivo()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync(_estandar1);
        var antes = TotalNotificaciones();

        // Actuar
        await NuevaGestion().RechazarAsync(solicitudId, _administrador1, Motivo);

        // Verificar
        var notificacion = Assert.Single(NotificacionesDe(_estandar1));
        Assert.Contains("rechazada", notificacion.Mensaje);
        Assert.Contains(Motivo, notificacion.Mensaje);
        Assert.Equal(antes + 1, TotalNotificaciones());
    }

    // RF-NOT-02
    [Fact]
    public async Task ResolucionRechazada_NoNotifica()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync(_estandar1);
        await NuevaGestion().RechazarAsync(solicitudId, _administrador1, Motivo);
        var antes = TotalNotificaciones();

        // Actuar
        var accion = () => NuevaGestion().AprobarAsync(solicitudId, _administrador2);

        // Verificar
        await Assert.ThrowsAsync<TransicionNoPermitidaException>(accion);
        Assert.Equal(antes, TotalNotificaciones());
    }

    // CA-NOT-06 · RF-NOT-04
    [Fact]
    public async Task Listar_DevuelveSoloLasPropiasDeLaMasRecienteALaMasAntigua()
    {
        // Preparar
        GuardarNotificacion(_estandar1, "Primera", Inicio, leida: true);
        GuardarNotificacion(_estandar1, "Segunda", Inicio.AddMinutes(10));
        GuardarNotificacion(_estandar1, "Tercera", Inicio.AddMinutes(20));
        GuardarNotificacion(_estandar2, "De otro", Inicio.AddMinutes(30));

        // Actuar
        var bandeja = await NuevaBandeja().ListarAsync(_estandar1, soloNoLeidas: false);
        var soloNoLeidas = await NuevaBandeja().ListarAsync(_estandar1, soloNoLeidas: true);

        // Verificar
        Assert.Equal(new[] { "Tercera", "Segunda", "Primera" }, bandeja.Notificaciones.Select(n => n.Asunto));
        Assert.Equal(new[] { false, false, true }, bandeja.Notificaciones.Select(n => n.EsLeida));
        Assert.Equal(2, bandeja.NoLeidas);
        Assert.Equal(new[] { "Tercera", "Segunda" }, soloNoLeidas.Notificaciones.Select(n => n.Asunto));
        Assert.Equal(2, soloNoLeidas.NoLeidas);

        var deOtro = await NuevaBandeja().ListarAsync(_estandar2, soloNoLeidas: false);
        Assert.Equal(new[] { "De otro" }, deOtro.Notificaciones.Select(n => n.Asunto));
    }

    // CA-NOT-07 · RF-NOT-05
    [Fact]
    public async Task MarcarLeida_Propia_GuardaLaFechaUnaSolaVez()
    {
        // Preparar
        GuardarNotificacion(_estandar1, "Aviso", Inicio);
        GuardarNotificacion(_estandar1, "Otro aviso", Inicio);
        var id = NotificacionesDe(_estandar1).Single(n => n.Asunto == "Aviso").Id;
        _reloj.Fijar(Inicio.AddHours(1));

        // Actuar
        var marcada = await NuevaBandeja().MarcarLeidaAsync(_estandar1, id);
        _reloj.Fijar(Inicio.AddHours(2));
        var otraVez = await NuevaBandeja().MarcarLeidaAsync(_estandar1, id);

        // Verificar
        Assert.True(marcada.EsLeida);
        Assert.Equal(Inicio.AddHours(1), marcada.FechaLecturaUtc);
        Assert.Equal(Inicio.AddHours(1), otraVez.FechaLecturaUtc);
        Assert.Equal(1, (await NuevaBandeja().ListarAsync(_estandar1, soloNoLeidas: false)).NoLeidas);
    }

    // CA-NOT-08 · RF-NOT-05
    [Fact]
    public async Task MarcarLeida_DeOtroUsuario_SeRechazaComoInexistente()
    {
        // Preparar
        GuardarNotificacion(_estandar1, "Privada", Inicio);
        var id = NotificacionesDe(_estandar1).Single().Id;

        // Actuar
        var deOtroEstandar = await Assert.ThrowsAsync<NotificacionNoEncontradaException>(
            () => NuevaBandeja().MarcarLeidaAsync(_estandar2, id));
        var deAdministrador = await Assert.ThrowsAsync<NotificacionNoEncontradaException>(
            () => NuevaBandeja().MarcarLeidaAsync(_administrador1, id));
        var inexistente = await Assert.ThrowsAsync<NotificacionNoEncontradaException>(
            () => NuevaBandeja().MarcarLeidaAsync(_estandar1, Guid.NewGuid()));

        // Verificar
        Assert.Equal("La notificación no existe.", deOtroEstandar.Message);
        Assert.Equal(inexistente.Message, deAdministrador.Message);
        Assert.False(NotificacionesDe(_estandar1).Single().EsLeida);
    }

    // CA-NOT-10 · RF-NOT-06
    [Fact]
    public async Task CadaNotificacion_EncolaUnCorreoConElMismoAsuntoYContenido()
    {
        // Preparar
        var solicitudId = await CrearSolicitudAsync(_estandar1);

        // Actuar
        await NuevaGestion().AprobarAsync(solicitudId, _administrador1);

        // Verificar
        foreach (var usuario in new[] { _administrador1, _administrador2, _estandar1 })
        {
            var notificacion = Assert.Single(NotificacionesDe(usuario));
            var correo = Assert.Single(CorreosDe(usuario));
            Assert.Equal(notificacion.Asunto, correo.Asunto);
            Assert.Equal(notificacion.Mensaje, correo.Cuerpo);
            Assert.Equal(EstadoCorreo.Pendiente, correo.Estado);
        }

        Assert.Empty(CorreosDe(_administradorInactivo));
        Assert.Empty(CorreosDe(_estandar2));
    }

    // CA-NOT-11 · RF-NOT-07
    [Fact]
    public async Task CorreoDesactivado_ConservaLaInternaYNoEncolaCorreo()
    {
        // Preparar
        await NuevaBandeja().ActualizarPreferenciasAsync(_estandar1, correoActivo: false);
        var solicitudId = await CrearSolicitudAsync(_estandar1);

        // Actuar
        await NuevaGestion().RechazarAsync(solicitudId, _administrador1, Motivo);

        // Verificar
        Assert.False((await NuevaBandeja().ObtenerPreferenciasAsync(_estandar1)).CorreoActivo);
        Assert.Contains(Motivo, Assert.Single(NotificacionesDe(_estandar1)).Mensaje);
        Assert.Empty(CorreosDe(_estandar1));
    }

    // CA-NOT-11 · RF-NOT-07
    [Fact]
    public async Task CorreoReactivado_LaSiguienteNotificacionVuelveAEncolarCorreo()
    {
        // Preparar
        await NuevaBandeja().ActualizarPreferenciasAsync(_estandar1, correoActivo: false);
        var primera = await CrearSolicitudAsync(_estandar1);
        await NuevaGestion().RechazarAsync(primera, _administrador1, Motivo);
        await NuevaBandeja().ActualizarPreferenciasAsync(_estandar1, correoActivo: true);
        var segunda = await CrearSolicitudAsync(_estandar1);

        // Actuar
        await NuevaGestion().AprobarAsync(segunda, _administrador1);

        // Verificar
        Assert.True((await NuevaBandeja().ObtenerPreferenciasAsync(_estandar1)).CorreoActivo);
        Assert.Equal(2, NotificacionesDe(_estandar1).Count);
        Assert.Contains("aprobada", Assert.Single(CorreosDe(_estandar1)).Cuerpo);
    }

    // CA-NOT-12 · RF-NOT-07
    [Fact]
    public async Task Preferencia_EsPersonalDeCadaUsuario()
    {
        // Preparar
        await NuevaBandeja().ActualizarPreferenciasAsync(_administrador1, correoActivo: false);

        // Actuar
        await CrearSolicitudAsync(_estandar1);

        // Verificar
        Assert.Single(NotificacionesDe(_administrador1));
        Assert.Single(NotificacionesDe(_administrador2));
        Assert.Empty(CorreosDe(_administrador1));
        Assert.Single(CorreosDe(_administrador2));
    }

    // RF-NOT-07
    [Fact]
    public async Task Preferencia_PorDefectoTieneElCorreoActivo()
    {
        // Actuar
        var preferencias = await NuevaBandeja().ObtenerPreferenciasAsync(_estandar2);

        // Verificar
        Assert.True(preferencias.CorreoActivo);
    }

    // CA-NOT-13 · RF-NOT-07
    [Fact]
    public async Task Recuperacion_SeEncolaAunqueElCorreoEsteDesactivadoYNoEsNotificacion()
    {
        // Preparar
        await NuevaBandeja().ActualizarPreferenciasAsync(_estandar1, correoActivo: false);
        string correo;
        using (var lectura = _baseDeDatos.NuevoContexto())
            correo = lectura.Usuarios.Single(u => u.Id == _estandar1).Correo;

        var contexto = _baseDeDatos.NuevoContexto();
        var servicio = new PasswordService(
            contexto,
            new HashSimple(),
            new InputValidator(),
            new ConfigurationBuilder().Build(),
            new EncoladorCorreos(contexto, _reloj),
            new Auditor(contexto, _reloj));

        // Actuar
        await servicio.OlvidePasswordAsync(correo);

        // Verificar
        Assert.Single(CorreosDe(_estandar1));
        Assert.Empty(NotificacionesDe(_estandar1));
    }
}
