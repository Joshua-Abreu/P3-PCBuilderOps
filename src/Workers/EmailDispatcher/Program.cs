using Core.Data;
using Core.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// RF-NOT-09: proceso independiente que procesa la cola una vez y termina.
var connectionString = CadenaConexion.Obtener();
var configuracionSmtp = ConfiguracionSmtp.DesdeEntorno();

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton(configuracionSmtp);
        services.AddSingleton(ConfiguracionColaCorreo.DesdeEntorno());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISmtpEmailSender, SmtpEmailSender>();
        services.AddScoped<IColaCorreoService, ColaCorreoService>();
    });

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

if (string.IsNullOrWhiteSpace(configuracionSmtp.Host))
    logger.LogWarning("SMTP_HOST no está configurado: los correos se marcarán con error.");

try
{
    var resultado = await scope.ServiceProvider.GetRequiredService<IColaCorreoService>().DespacharPendientesAsync();
    logger.LogInformation("Despacho completado. Enviados: {Enviados}. Con error: {ConError}.", resultado.Enviados, resultado.ConError);
    return 0;
}
catch (Exception ex)
{
    logger.LogError("Error durante el despacho de correos: {Error}", ex.Message);
    return 1;
}
