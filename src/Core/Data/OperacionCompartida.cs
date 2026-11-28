using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.Data;

// RF-AUD-01, RF-NOT-03, RD-03
public class OperacionCompartida : IOperacionCompartida
{
    private readonly AppDbContext _dbContext;

    public OperacionCompartida(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> EjecutarAsync<T>(DbContext contextoDelCambio, Func<CancellationToken, Task<T>> cambio, CancellationToken ct = default)
    {
        if (!ReferenceEquals(contextoDelCambio.Database.GetDbConnection(), _dbContext.Database.GetDbConnection()))
            throw new ConexionNoCompartidaException();

        await using var transaccion = await _dbContext.Database.BeginTransactionAsync(ct);
        await contextoDelCambio.Database.UseTransactionAsync(transaccion.GetDbTransaction(), ct);

        try
        {
            var resultado = await cambio(ct);

            await _dbContext.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);

            return resultado;
        }
        finally
        {
            await contextoDelCambio.Database.UseTransactionAsync(null, CancellationToken.None);
        }
    }
}
