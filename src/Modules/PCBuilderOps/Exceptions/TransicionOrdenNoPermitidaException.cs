using PCBuilderOps.Domain;

namespace PCBuilderOps.Exceptions;

public class TransicionOrdenNoPermitidaException : Exception
{
    public TransicionOrdenNoPermitidaException(EstadoOrdenEnsamble desde, EstadoOrdenEnsamble hacia)
        : base($"La orden está {desde} y no puede pasar a {hacia}.")
    {
        Desde = desde;
        Hacia = hacia;
    }

    public EstadoOrdenEnsamble Desde { get; }
    public EstadoOrdenEnsamble Hacia { get; }
}
