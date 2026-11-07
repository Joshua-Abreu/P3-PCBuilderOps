namespace PCBuilderOps.TasaCambio;

public class TasaCambioNoDisponibleException : Exception
{
    public TasaCambioNoDisponibleException(string mensaje) : base(mensaje) { }
}
