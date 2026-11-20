namespace Core.Notifications.Exceptions;

public class NotificacionNoEncontradaException : Exception
{
    public NotificacionNoEncontradaException()
        : base("La notificación no existe.") { }
}
