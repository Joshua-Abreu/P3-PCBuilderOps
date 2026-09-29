using Core.Data;
using Core.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Notifications.Workers;

/// <summary>
/// Worker en segundo plano que procesa la cola de correos.
/// RF-NOT-12: No duplicación — solo procesa correos Pendientes.
/// RF-NOT-13: Envío asíncrono sin bloquear la aplicación.
/// RD-11: Fechas en UTC.
/// </summary>
public class EmailQueueWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailQueueWorker> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromSeconds(10);

    public EmailQueueWorker(
        IServiceProvider serviceProvider,
        ILogger<EmailQueueWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueWorker iniciado. Intervalo: {Intervalo}s", _intervalo.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarColaAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // RD-08: Log limpio, sin interrumpir el proceso
                _logger.LogError(ex, "Error procesando cola de correos");
            }

            await Task.Delay(_intervalo, stoppingToken);
        }
    }

    private async Task ProcesarColaAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        // RF-NOT-12: Solo correos Pendientes (no duplicación)
        var pendientes = await dbContext.CorreosEnCola
            .Where(c => c.Estado == "Pendiente")
            .OrderBy(c => c.FechaCreacionUtc)
            .ToListAsync(ct);

        if (pendientes.Count == 0)
            return;

        _logger.LogInformation("Procesando {Count} correos pendientes", pendientes.Count);

        foreach (var correo in pendientes)
        {
            try
            {
                await emailSender.EnviarAsync(correo.Destinatario, correo.Asunto, correo.Cuerpo, ct);

                // RF-NOT-12: Marcar como Enviado inmediatamente
                correo.Estado = "Enviado";
                correo.FechaEnvioUtc = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(ct);

                _logger.LogInformation("Correo enviado a {Destinatario}", correo.Destinatario);
            }
            catch (Exception ex)
            {
                // Si falla, permanece Pendiente para el siguiente ciclo
                _logger.LogWarning(ex, "No se pudo enviar correo a {Destinatario}. Reintentando en próximo ciclo.", correo.Destinatario);
            }
        }
    }
}
