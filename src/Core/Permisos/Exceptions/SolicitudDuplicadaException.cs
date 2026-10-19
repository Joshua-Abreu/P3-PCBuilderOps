namespace Core.Permisos.Exceptions;

public class SolicitudDuplicadaException : Exception
{
    public SolicitudDuplicadaException(string mensaje) : base(mensaje) { }
}
