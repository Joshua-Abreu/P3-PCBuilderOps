using Core.Auth.Exceptions;
using Core.Data;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Notifications.Services;

public class ColaCorreoService : IColaCorreoService
{
    private readonly AppDbContext _dbContext;
    private readonly ISmtpEmailSender _smtpEmailSender;
    private readonly ConfiguracionColaCorreo _configuracion;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ColaCorreoService> _logger;

    public ColaCorreoService(
        AppDbContext dbContext,
        ISmtpEmailSender smtpEmailSender,
        ConfiguracionColaCorreo configuracion,
        TimeProvider reloj,
        ILogger<ColaCorreoService> logger)
    {
        _dbContext = dbContext;
        _smtpEmailSender = smtpEmailSender;
        _configuracion = configuracion;
        _reloj = reloj;
        _logger = logger;
    }

    private DateTime AhoraUtc => _reloj.GetUtcNow().UtcDateTime;

    // RF-NOT-09
    public async Task<ResultadoDespacho> DespacharPendientesAsync(CancellationToken ct = default)
    {
        var ahora = AhoraUtc;
        var candidatos = await Disponibles(ahora)
            .OrderBy(c => c.FechaCreacionUtc)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var enviados = 0;
        var conError = 0;

        foreach (var id in candidatos)
        {
            if (!await ReservarAsync(id, ct))
                continue;

            var correo = await _dbContext.CorreosEnCola.SingleAsync(c => c.Id == id, ct);

            try
            {
                await _smtpEmailSender.EnviarAsync(correo.Destinatario, correo.Asunto, correo.Cuerpo, ct);
                correo.MarcarEnviado(AhoraUtc);
                enviados++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                correo.RegistrarFallo(ex.Message, _configuracion.MaximoIntentos, AhoraUtc);
                conError++;
                _logger.LogWarning(
                    "No se pudo enviar el correo {CorreoId} (intento {Intentos} de {Maximo}): {Error}",
                    correo.Id, correo.Intentos, _configuracion.MaximoIntentos, correo.UltimoError);
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        return new ResultadoDespacho(enviados, conError);
    }

    // RF-NOT-12: solo un procesador logra reservar cada correo, aunque corran dos a la vez.
    private async Task<bool> ReservarAsync(Guid id, CancellationToken ct)
    {
        var ahora = AhoraUtc;
        DateTime? reservadoHasta = ahora + _configuracion.DuracionReserva;

        var reservados = await Disponibles(ahora)
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ReservadoHastaUtc, reservadoHasta), ct);

        return reservados == 1;
    }

    private IQueryable<CorreoEnCola> Disponibles(DateTime ahora) =>
        _dbContext.CorreosEnCola.Where(c =>
            c.Estado == EstadoCorreo.Pendiente && (c.ReservadoHastaUtc == null || c.ReservadoHastaUtc < ahora));

    // RF-NOT-11
    public async Task<IReadOnlyList<CorreoResumen>> ListarAsync(string? estado, string? destinatario, CancellationToken ct = default)
    {
        var consulta = _dbContext.CorreosEnCola.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            var estadoValido = EstadoCorreo.Todos.FirstOrDefault(e => string.Equals(e, estado.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new ValidacionException($"El estado no es válido. Usa {string.Join(", ", EstadoCorreo.Todos)}.");
            consulta = consulta.Where(c => c.Estado == estadoValido);
        }

        if (!string.IsNullOrWhiteSpace(destinatario))
        {
            var correo = destinatario.Trim().ToLowerInvariant();
            consulta = consulta.Where(c => c.Destinatario == correo);
        }

        return await consulta
            .OrderByDescending(c => c.FechaCreacionUtc)
            .Select(c => new CorreoResumen(
                c.Id,
                c.Destinatario,
                c.Asunto,
                c.Estado,
                c.Intentos,
                c.FechaCreacionUtc,
                c.FechaUltimoIntentoUtc,
                c.FechaEnvioUtc,
                c.UltimoError))
            .ToListAsync(ct);
    }
}
