using Core.Auth.Exceptions;
using Core.Notifications.Domain;
using Core.Notifications.Services;
using Core.Tests.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Notifications;

public sealed class ColaCorreoServiceTests : IDisposable
{
    private const int MaximoIntentos = 3;

    private sealed class RemitenteFalso : ISmtpEmailSender
    {
        public List<string> Enviados { get; } = [];
        public int Llamadas { get; private set; }
        public int FallosRestantes { get; set; }
        public Func<Task>? AlEnviar { get; set; }

        public async Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default)
        {
            Llamadas++;

            if (AlEnviar is not null)
                await AlEnviar();

            if (FallosRestantes != 0)
            {
                if (FallosRestantes > 0)
                    FallosRestantes--;
                throw new InvalidOperationException("Conexión rechazada por el servidor de correo.");
            }

            Enviados.Add(destinatario);
        }
    }

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly RelojFijo _reloj = new(new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc));
    private readonly RemitenteFalso _remitente = new();

    public void Dispose() => _baseDeDatos.Dispose();

    private ColaCorreoService NuevoServicio() => new(
        _baseDeDatos.NuevoContexto(),
        _remitente,
        new ConfiguracionColaCorreo(MaximoIntentos, TimeSpan.FromMinutes(5)),
        _reloj,
        NullLogger<ColaCorreoService>.Instance);

    private void Encolar(params string[] destinatarios)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        var encolador = new EncoladorCorreos(contexto, _reloj);

        foreach (var destinatario in destinatarios)
            encolador.Encolar(destinatario, "Asunto", "Cuerpo");

        contexto.SaveChanges();
    }

    private CorreoEnCola Correo(string destinatario)
    {
        using var contexto = _baseDeDatos.NuevoContexto();
        return contexto.CorreosEnCola.AsNoTracking().Single(c => c.Destinatario == destinatario);
    }

    // CA-COLA-01 · RF-NOT-08
    [Fact]
    public void Encolar_GuardaElCorreoPendienteSinEnviarlo()
    {
        Encolar("a@prueba.com");

        var correo = Correo("a@prueba.com");
        Assert.Equal(EstadoCorreo.Pendiente, correo.Estado);
        Assert.Equal(0, correo.Intentos);
        Assert.Null(correo.FechaEnvioUtc);
        Assert.Equal(0, _remitente.Llamadas);
    }

    // CA-COLA-02 · RF-NOT-09
    [Fact]
    public async Task Despachar_EnviaLosPendientesYLosMarcaEnviados()
    {
        Encolar("a@prueba.com", "b@prueba.com");

        var resultado = await NuevoServicio().DespacharPendientesAsync();

        Assert.Equal(new ResultadoDespacho(2, 0), resultado);
        Assert.Equal(["a@prueba.com", "b@prueba.com"], _remitente.Enviados.Order());
        var correo = Correo("a@prueba.com");
        Assert.Equal(EstadoCorreo.Enviado, correo.Estado);
        Assert.Equal(1, correo.Intentos);
        Assert.Equal(_reloj.GetUtcNow().UtcDateTime, correo.FechaEnvioUtc);
    }

    // CA-COLA-03 · RF-NOT-10
    [Fact]
    public async Task Despachar_ConServidorCaido_ReintentaElMaximoYQuedaFallido()
    {
        _remitente.FallosRestantes = -1;
        Encolar("a@prueba.com");

        for (var pasada = 1; pasada < MaximoIntentos; pasada++)
        {
            Assert.Equal(new ResultadoDespacho(0, 1), await NuevoServicio().DespacharPendientesAsync());
            var pendiente = Correo("a@prueba.com");
            Assert.Equal(EstadoCorreo.Pendiente, pendiente.Estado);
            Assert.Equal(pasada, pendiente.Intentos);
            Assert.Equal("Conexión rechazada por el servidor de correo.", pendiente.UltimoError);
            Assert.NotNull(pendiente.FechaUltimoIntentoUtc);
        }

        await NuevoServicio().DespacharPendientesAsync();
        Assert.Equal(EstadoCorreo.Fallido, Correo("a@prueba.com").Estado);

        var cuartaPasada = await NuevoServicio().DespacharPendientesAsync();

        var fallido = Correo("a@prueba.com");
        Assert.Equal(new ResultadoDespacho(0, 0), cuartaPasada);
        Assert.Equal(MaximoIntentos, fallido.Intentos);
        Assert.Equal(MaximoIntentos, _remitente.Llamadas);
        Assert.Equal(EstadoCorreo.Fallido, fallido.Estado);
    }

    // CA-COLA-04 · RF-NOT-10
    [Fact]
    public async Task Despachar_CorreoQueFalloUnaVez_SeEnviaAlRecuperarseElServidor()
    {
        _remitente.FallosRestantes = 1;
        Encolar("a@prueba.com");

        await NuevoServicio().DespacharPendientesAsync();
        await NuevoServicio().DespacharPendientesAsync();

        var correo = Correo("a@prueba.com");
        Assert.Equal(EstadoCorreo.Enviado, correo.Estado);
        Assert.Equal(2, correo.Intentos);
        Assert.NotNull(correo.UltimoError);
        Assert.Single(_remitente.Enviados);
    }

    // CA-COLA-03 · RF-NOT-11
    [Fact]
    public void RegistrarFallo_RecortaElErrorAlLargoMaximo()
    {
        var correo = CorreoEnCola.Nuevo("a@prueba.com", "Asunto", "Cuerpo", DateTime.UtcNow);

        correo.RegistrarFallo(new string('x', CorreoEnCola.LargoMaximoError + 50), MaximoIntentos, DateTime.UtcNow);

        Assert.Equal(CorreoEnCola.LargoMaximoError, correo.UltimoError!.Length);
    }

    // CA-COLA-07 · RF-NOT-12
    [Fact]
    public async Task Despachar_DosVecesSeguidas_NoDuplicaEnvios()
    {
        Encolar("a@prueba.com", "b@prueba.com");

        var primera = await NuevoServicio().DespacharPendientesAsync();
        var fechaEnvio = Correo("a@prueba.com").FechaEnvioUtc;
        _reloj.Fijar(new DateTime(2026, 11, 19, 12, 5, 0, DateTimeKind.Utc));
        var segunda = await NuevoServicio().DespacharPendientesAsync();

        Assert.Equal(2, primera.Enviados);
        Assert.Equal(new ResultadoDespacho(0, 0), segunda);
        Assert.Equal(2, _remitente.Enviados.Count);
        Assert.Equal(fechaEnvio, Correo("a@prueba.com").FechaEnvioUtc);
    }

    // CA-COLA-08 · RF-NOT-12
    [Fact]
    public async Task Despachar_DosProcesadoresALaVez_CadaCorreoSeEnviaUnaSolaVez()
    {
        Encolar("a@prueba.com", "b@prueba.com");
        ResultadoDespacho? resultadoDelSegundo = null;
        _remitente.AlEnviar = async () =>
        {
            _remitente.AlEnviar = null;
            resultadoDelSegundo = await NuevoServicio().DespacharPendientesAsync();
        };

        var resultadoDelPrimero = await NuevoServicio().DespacharPendientesAsync();

        Assert.Equal(2, resultadoDelPrimero.Enviados + resultadoDelSegundo!.Enviados);
        Assert.Equal(["a@prueba.com", "b@prueba.com"], _remitente.Enviados.Order());
        Assert.Equal(1, Correo("a@prueba.com").Intentos);
        Assert.Equal(1, Correo("b@prueba.com").Intentos);
    }

    // RF-NOT-12: un correo reservado por un procesador que se detuvo vuelve a la cola al vencer la reserva.
    [Fact]
    public async Task Despachar_ReservaVencida_ElCorreoSeVuelveATomar()
    {
        Encolar("a@prueba.com");
        using (var contexto = _baseDeDatos.NuevoContexto())
        {
            DateTime? reservadoHasta = _reloj.GetUtcNow().UtcDateTime.AddMinutes(5);
            await contexto.CorreosEnCola.ExecuteUpdateAsync(s => s.SetProperty(c => c.ReservadoHastaUtc, reservadoHasta));
        }

        var mientrasReservado = await NuevoServicio().DespacharPendientesAsync();
        _reloj.Fijar(_reloj.GetUtcNow().UtcDateTime.AddMinutes(6));
        var alVencer = await NuevoServicio().DespacharPendientesAsync();

        Assert.Equal(0, mientrasReservado.Enviados);
        Assert.Equal(1, alVencer.Enviados);
    }

    // CA-COLA-05 · RF-NOT-11
    [Fact]
    public async Task Listar_FiltraPorEstadoYDestinatarioDelMasRecienteAlMasAntiguo()
    {
        Encolar("a@prueba.com");
        _reloj.Fijar(_reloj.GetUtcNow().UtcDateTime.AddMinutes(1));
        Encolar("b@prueba.com");
        _remitente.FallosRestantes = -1;
        await NuevoServicio().DespacharPendientesAsync();
        _reloj.Fijar(_reloj.GetUtcNow().UtcDateTime.AddMinutes(1));
        Encolar("c@prueba.com");

        var todos = await NuevoServicio().ListarAsync(null, null);
        var pendientes = await NuevoServicio().ListarAsync("pendiente", null);
        var deB = await NuevoServicio().ListarAsync(null, " B@prueba.com ");

        Assert.Equal(["c@prueba.com", "b@prueba.com", "a@prueba.com"], todos.Select(c => c.Destinatario));
        Assert.Equal(3, pendientes.Count);
        var correoB = Assert.Single(deB);
        Assert.Equal(1, correoB.Intentos);
        Assert.Equal("Conexión rechazada por el servidor de correo.", correoB.UltimoError);
        Assert.Empty(await NuevoServicio().ListarAsync(EstadoCorreo.Enviado, null));
    }

    // CA-COLA-05 · RD-07
    [Fact]
    public async Task Listar_EstadoInvalido_SeRechaza()
    {
        var error = await Assert.ThrowsAsync<ValidacionException>(() => NuevoServicio().ListarAsync("Perdido", null));

        Assert.Equal("El estado no es válido. Usa Pendiente, Enviado, Fallido.", error.Message);
    }
}
