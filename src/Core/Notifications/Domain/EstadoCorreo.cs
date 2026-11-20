namespace Core.Notifications.Domain;

// RF-NOT-11
public static class EstadoCorreo
{
    public const string Pendiente = "Pendiente";
    public const string Enviado = "Enviado";
    public const string Fallido = "Fallido";

    public static readonly IReadOnlyList<string> Todos = [Pendiente, Enviado, Fallido];
}
