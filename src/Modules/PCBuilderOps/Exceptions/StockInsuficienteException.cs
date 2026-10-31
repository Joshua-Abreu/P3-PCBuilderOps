namespace PCBuilderOps.Exceptions;

public class StockInsuficienteException : Exception
{
    public StockInsuficienteException(string componente)
        : base($"No hay stock suficiente de {componente}.")
    {
    }
}
