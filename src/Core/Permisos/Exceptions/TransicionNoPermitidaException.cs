using Core.Permisos.Domain;

namespace Core.Permisos.Exceptions;

public class TransicionNoPermitidaException : Exception
{
    public TransicionNoPermitidaException(EstadoSolicitud desde, EstadoSolicitud hacia)
        : base($"La solicitud está {desde} y no puede pasar a {hacia}.")
    {
        Desde = desde;
        Hacia = hacia;
    }

    public EstadoSolicitud Desde { get; }
    public EstadoSolicitud Hacia { get; }
}
