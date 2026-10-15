using Core.Data;
using Core.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var connectionString = CadenaConexion.Obtener();

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMTP_HOST")))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[SMTP] ADVERTENCIA: SMTP_HOST no está configurado. El envío de correos no funcionará.");
    Console.ResetColor();
}

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddSingleton<ISmtpEmailSender, SmtpEmailSender>();
        services.AddScoped<IColaCorreoService, ColaCorreoService>();
        services.AddLogging(logging => logging.AddConsole());
    });

var host = builder.Build();

using var scope = host.Services.CreateScope();
var colaCorreoService = scope.ServiceProvider.GetRequiredService<IColaCorreoService>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Iniciando despachador de correos...");

try
{
    var enviados = await colaCorreoService.DespacharPendientesAsync(CancellationToken.None);
    logger.LogInformation("Despacho completado. Correos enviados: {Enviados}", enviados);
    return enviados >= 0 ? 0 : 1;
}
catch (Exception ex)
{
    logger.LogError(ex, "Error durante el despacho de correos");
    return 1;
}
