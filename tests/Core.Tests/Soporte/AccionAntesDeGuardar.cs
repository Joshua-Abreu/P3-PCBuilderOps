using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Core.Tests.Soporte;

public sealed class AccionAntesDeGuardar(Func<Task> otraPeticion) : SaveChangesInterceptor
{
    private bool _yaSeEjecuto;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_yaSeEjecuto)
        {
            _yaSeEjecuto = true;
            await otraPeticion();
        }

        return result;
    }
}
