using Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Notifications.Services;

public class ColaCorreoService : IColaCorreoService
{
    private readonly AppDbContext _dbContext;
    private readonly ISmtpEmailSender _smtpEmailSender;
    private readonly ILogger<ColaCorreoService> _logger;

    public ColaCorreoService(
        AppDbContext dbContext,
        ISmtpEmailSender smtpEmailSender,
        ILogger<ColaCorreoService> logger)
    {
        _dbContext = dbContext;
        _smtpEmailSender = smtpEmailSender;
        _logger = logger;
    }

    public async Task<int> DespacharPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _dbContext.CorreosEnCola
            .Where(c => c.Estado == "Pendiente")
            .OrderBy(c => c.FechaCreacionUtc)
            .ToListAsync(ct);

        if (pendientes.Count == 0)
            return 0;

        _logger.LogInformation("Despachando {Count} correos pendientes", pendientes.Count);

        var enviados = 0;

        foreach (var correo in pendientes)
        {
            try
            {
                await _smtpEmailSender.EnviarAsync(correo.Destinatario, correo.Asunto, correo.Cuerpo, ct);

                correo.Estado = "Enviado";
                correo.FechaEnvioUtc = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(ct);
                enviados++;

                _logger.LogInformation("Correo enviado a {Destinatario}", correo.Destinatario);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar correo a {Destinatario}. Permanece pendiente.", correo.Destinatario);
            }
        }

        return enviados;
    }
}
