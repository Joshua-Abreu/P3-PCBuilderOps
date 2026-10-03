using Core.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Notifications.Workers;

public class EmailQueueWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailQueueWorker> _logger;
    private readonly TimeSpan _intervalo;

    public EmailQueueWorker(
        IServiceProvider serviceProvider,
        ILogger<EmailQueueWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var intervaloSegundos = int.TryParse(
            Environment.GetEnvironmentVariable("EMAIL_WORKER_INTERVALO_SEGUNDOS"),
            out var seg) ? seg : 30;
        _intervalo = TimeSpan.FromSeconds(intervaloSegundos);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueWorker iniciado. Intervalo: {Intervalo}s", _intervalo.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var colaCorreoService = scope.ServiceProvider.GetRequiredService<IColaCorreoService>();
                await colaCorreoService.DespacharPendientesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando cola de correos");
            }

            await Task.Delay(_intervalo, stoppingToken);
        }
    }
}
