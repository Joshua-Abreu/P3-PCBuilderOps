using Core.Data;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;

namespace API.Arranque;

// RD-09: en el contenedor no hay pasos manuales; el Core migra primero porque el módulo referencia Usuarios.
public static class MigracionesIniciales
{
    private const int IntentosMaximos = 30;
    private static readonly TimeSpan EsperaEntreIntentos = TimeSpan.FromSeconds(2);

    public static async Task AplicarAsync(IServiceProvider servicios, ILogger logger, CancellationToken ct = default)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                using var scope = servicios.CreateScope();
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(ct);
                await scope.ServiceProvider.GetRequiredService<PCBuilderOpsDbContext>().Database.MigrateAsync(ct);

                logger.LogInformation("Migraciones aplicadas.");
                return;
            }
            catch (Exception ex) when (intento < IntentosMaximos && !ct.IsCancellationRequested)
            {
                logger.LogWarning("La base de datos aún no responde (intento {Intento} de {Maximo}): {Motivo}",
                    intento, IntentosMaximos, ex.Message);

                await Task.Delay(EsperaEntreIntentos, ct);
            }
        }
    }
}
