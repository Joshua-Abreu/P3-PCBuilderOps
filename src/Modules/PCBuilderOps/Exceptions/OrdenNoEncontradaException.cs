namespace PCBuilderOps.Exceptions;

public class OrdenNoEncontradaException : Exception
{
    public OrdenNoEncontradaException() : base("La orden no existe.")
    {
    }
}
