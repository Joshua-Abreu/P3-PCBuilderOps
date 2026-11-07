using System.Text.RegularExpressions;
using Core.Auth.Exceptions;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Exceptions;
using PCBuilderOps.TasaCambio;

namespace PCBuilderOps.Services;

// FN-01: el costo de la cotización expresado en otra moneda, con la tasa de una API externa.
public partial class ConversionCostoService : IConversionCostoService
{
    public const string MonedaDeLasOrdenes = "USD";
    private const string Fuente = "ExchangeRate-API";

    private readonly PCBuilderOpsDbContext _dbContext;
    private readonly IProveedorTasaCambio _proveedor;

    public ConversionCostoService(PCBuilderOpsDbContext dbContext, IProveedorTasaCambio proveedor)
    {
        _dbContext = dbContext;
        _proveedor = proveedor;
    }

    public async Task<CostoConvertido> ConvertirAsync(ActorTaller actor, Guid ordenId, string? moneda, CancellationToken ct = default)
    {
        var monedaDestino = moneda?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!CodigoMoneda().IsMatch(monedaDestino))
            throw new ValidacionException("Indica la moneda con su código de tres letras, por ejemplo ?moneda=DOP.");

        var orden = await _dbContext.OrdenesEnsamble
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ordenId, ct);

        if (orden is null || (!actor.EsAdministrador && orden.ClienteUsuarioId != actor.UsuarioId))
            throw new OrdenNoEncontradaException();

        var tasa = await _proveedor.ObtenerAsync(MonedaDeLasOrdenes, monedaDestino, ct);

        return new CostoConvertido(
            orden.Id,
            orden.CodigoSeguimiento,
            MonedaDeLasOrdenes,
            orden.CostoTotal,
            monedaDestino,
            tasa.Valor,
            Math.Round(orden.CostoTotal * tasa.Valor, 2, MidpointRounding.AwayFromZero),
            tasa.FechaActualizacionUtc,
            Fuente);
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CodigoMoneda();
}
