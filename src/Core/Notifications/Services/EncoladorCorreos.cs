using Core.Data;
using Core.Notifications.Domain;

namespace Core.Notifications.Services;

// RF-NOT-08
public class EncoladorCorreos : IEncoladorCorreos
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _reloj;

    public EncoladorCorreos(AppDbContext dbContext, TimeProvider reloj)
    {
        _dbContext = dbContext;
        _reloj = reloj;
    }

    public void Encolar(string destinatario, string asunto, string cuerpo) =>
        _dbContext.CorreosEnCola.Add(CorreoEnCola.Nuevo(destinatario, asunto, cuerpo, _reloj.GetUtcNow().UtcDateTime));
}
