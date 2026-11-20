namespace Core.Notifications.Services;

// RF-NOT-06
public interface INotificador
{
    Task NotificarAsync(Guid usuarioId, string asunto, string mensaje, CancellationToken ct = default);
    Task NotificarAdministradoresAsync(string asunto, string mensaje, Guid? excepto = null, CancellationToken ct = default);

    // RF-NOT-03: para quien guarda su cambio en otro DbContext, como el módulo del negocio.
    Task NotificarYGuardarAsync(Guid usuarioId, string asunto, string mensaje, CancellationToken ct = default);
}
