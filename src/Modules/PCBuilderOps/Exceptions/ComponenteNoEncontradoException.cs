namespace PCBuilderOps.Exceptions;

public class ComponenteNoEncontradoException : Exception
{
    public ComponenteNoEncontradoException() : base("El componente no existe.")
    {
    }
}
