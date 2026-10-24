using Core.Permisos.Exceptions;

namespace Core.Permisos.Domain;

// RD-04, RF-GP-06
public static class MaquinaEstadosSolicitud
{
    private static readonly Dictionary<EstadoSolicitud, HashSet<EstadoSolicitud>> TransicionesValidas = new()
    {
        [EstadoSolicitud.Pendiente] = [EstadoSolicitud.Aprobada, EstadoSolicitud.Rechazada],
        [EstadoSolicitud.Aprobada] = [EstadoSolicitud.Aplicada],
        [EstadoSolicitud.Aplicada] = [EstadoSolicitud.Aprobada],
        [EstadoSolicitud.Rechazada] = []
    };

    public static bool EsTransicionValida(EstadoSolicitud desde, EstadoSolicitud hacia) =>
        TransicionesValidas.TryGetValue(desde, out var destinos) && destinos.Contains(hacia);

    public static void ValidarTransicion(EstadoSolicitud desde, EstadoSolicitud hacia)
    {
        if (!EsTransicionValida(desde, hacia))
            throw new TransicionNoPermitidaException(desde, hacia);
    }
}
