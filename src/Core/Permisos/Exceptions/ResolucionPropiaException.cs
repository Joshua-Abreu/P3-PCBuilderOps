namespace Core.Permisos.Exceptions;

public class ResolucionPropiaException : Exception
{
    public ResolucionPropiaException()
        : base("No puedes resolver una solicitud creada por ti mismo.") { }
}
