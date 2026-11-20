using Core.Auth.Domain;
using Core.Data;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Notifications.Services;

// RF-NOT-06, RF-NOT-07
public class Notificador : INotificador
{
    private readonly AppDbContext _dbContext;
    private readonly IEncoladorCorreos _encolador;
    private readonly TimeProvider _reloj;

    public Notificador(AppDbContext dbContext, IEncoladorCorreos encolador, TimeProvider reloj)
    {
        _dbContext = dbContext;
        _encolador = encolador;
        _reloj = reloj;
    }

    public Task NotificarAsync(Guid usuarioId, string asunto, string mensaje, CancellationToken ct = default) =>
        NotificarAsync(_dbContext.Usuarios.Where(u => u.Id == usuarioId), asunto, mensaje, ct);

    // RF-NOT-01
    public Task NotificarAdministradoresAsync(string asunto, string mensaje, Guid? excepto = null, CancellationToken ct = default) =>
        NotificarAsync(
            _dbContext.Usuarios.Where(u => u.Rol == RolUsuario.Administrador && u.EstaActivo && u.Id != excepto),
            asunto,
            mensaje,
            ct);

    private async Task NotificarAsync(IQueryable<Usuario> destinatarios, string asunto, string mensaje, CancellationToken ct)
    {
        var usuarios = await destinatarios
            .AsNoTracking()
            .Select(u => new { u.Id, u.Correo })
            .ToListAsync(ct);

        if (usuarios.Count == 0)
            return;

        var ids = usuarios.Select(u => u.Id).ToList();
        var sinCorreo = await _dbContext.PreferenciasNotificacion
            .AsNoTracking()
            .Where(p => ids.Contains(p.UsuarioId) && !p.CorreoActivo)
            .Select(p => p.UsuarioId)
            .ToListAsync(ct);

        var ahora = _reloj.GetUtcNow().UtcDateTime;

        foreach (var usuario in usuarios)
        {
            var notificacion = Notificacion.Nueva(usuario.Id, asunto, mensaje, ahora);
            _dbContext.Notificaciones.Add(notificacion);

            if (!sinCorreo.Contains(usuario.Id))
                _encolador.Encolar(usuario.Correo, notificacion.Asunto, notificacion.Mensaje);
        }
    }
}
