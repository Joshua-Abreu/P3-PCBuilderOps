namespace PCBuilderOps.Exceptions;

public class ImagenNoEncontradaException : Exception
{
    public ImagenNoEncontradaException() : base("El componente no tiene imagen.")
    {
    }
}
