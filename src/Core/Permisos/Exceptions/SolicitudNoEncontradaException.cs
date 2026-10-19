namespace Core.Permisos.Exceptions;

public class SolicitudNoEncontradaException : Exception
{
    public SolicitudNoEncontradaException()
        : base("La solicitud de permiso no existe.") { }
}
