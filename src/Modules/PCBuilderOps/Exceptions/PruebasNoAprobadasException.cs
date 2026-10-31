namespace PCBuilderOps.Exceptions;

public class PruebasNoAprobadasException : Exception
{
    public PruebasNoAprobadasException()
        : base("La orden necesita al menos una prueba de rendimiento y la más reciente debe estar aprobada.")
    {
    }
}
