using Microsoft.EntityFrameworkCore;

namespace Core.Data;

// RF-AUD-01, RF-NOT-03, RD-03
public interface IOperacionCompartida
{
    Task<T> EjecutarAsync<T>(DbContext contextoDelCambio, Func<CancellationToken, Task<T>> cambio, CancellationToken ct = default);
}

public class ConexionNoCompartidaException : InvalidOperationException
{
    public ConexionNoCompartidaException()
        : base("El contexto del cambio debe compartir la conexión del Core para guardarse en la misma operación.")
    {
    }
}
