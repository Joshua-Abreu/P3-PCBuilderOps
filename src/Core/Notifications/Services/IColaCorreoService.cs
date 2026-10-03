namespace Core.Notifications.Services;

public interface IColaCorreoService
{
    Task<int> DespacharPendientesAsync(CancellationToken ct = default);
}
