namespace Core.Notifications.Services;

// RF-NOT-08, RD-01: los demás componentes encolan por aquí; el correo se guarda con el SaveChanges de quien lo origina.
public interface IEncoladorCorreos
{
    void Encolar(string destinatario, string asunto, string cuerpo);
}
