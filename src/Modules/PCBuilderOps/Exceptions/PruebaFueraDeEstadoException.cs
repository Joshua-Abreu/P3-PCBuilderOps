using PCBuilderOps.Domain;

namespace PCBuilderOps.Exceptions;

public class PruebaFueraDeEstadoException : Exception
{
    public PruebaFueraDeEstadoException(EstadoOrdenEnsamble estado)
        : base($"La orden está {estado}; solo se registran pruebas cuando está {EstadoOrdenEnsamble.EnPruebas}.")
    {
    }
}
