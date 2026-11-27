using Core.Auditoria.Services;
using Core.Data;
using Core.Notifications.Services;
using Core.Permisos.Services;

namespace Core.Tests.Soporte;

public static class Servicios
{
    public static GestionPermisosService GestionPermisos(AppDbContext contexto, TimeProvider reloj) =>
        new(contexto, reloj, new Notificador(contexto, new EncoladorCorreos(contexto, reloj), reloj), new Auditor(contexto, reloj));
}
