using Core.Auth.Domain;
using Core.Auth.Exceptions;
using PCBuilderOps.Data;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Services;
using PCBuilderOps.TasaCambio;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// FN-01: la API externa se reemplaza por una tasa fija; el proveedor real tiene sus propias pruebas.
public sealed class ConversionCostoServiceTests : IDisposable
{
    private static readonly DateTime FechaTasa = new(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc);

    private sealed class ProveedorFijo : IProveedorTasaCambio
    {
        public decimal Tasa { get; init; } = 60m;
        public Exception? Error { get; init; }
        public int Llamadas { get; private set; }

        public Task<TasaDeCambio> ObtenerAsync(string monedaOrigen, string monedaDestino, CancellationToken ct = default)
        {
            Llamadas++;
            return Error is null
                ? Task.FromResult(new TasaDeCambio(monedaOrigen, monedaDestino, Tasa, FechaTasa))
                : Task.FromException<TasaDeCambio>(Error);
        }
    }

    private readonly BaseDeDatosDelNegocio _bd = new();
    private readonly Guid _clienteId;
    private readonly Guid _administradorId;

    public ConversionCostoServiceTests()
    {
        _clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
        _administradorId = _bd.CrearUsuario(RolUsuario.Administrador);
    }

    public void Dispose() => _bd.Dispose();

    private ConversionCostoService NuevoServicio(IProveedorTasaCambio proveedor) => new(_bd.NuevoContextoNegocio(), proveedor);

    private Task<OrdenDetalle> CotizarAsync() => _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId,
    [
        new LineaCotizacion(ComponentesIniciales.CpuAm5, 1),
        new LineaCotizacion(ComponentesIniciales.PlacaAm5, 1),
        new LineaCotizacion(ComponentesIniciales.Fuente850W, 1)
    ]);

    [Fact]
    public async Task Convertir_DuenoDeLaOrden_CalculaElCostoConLaTasa()
    {
        // Preparar
        var orden = await CotizarAsync();
        var servicio = NuevoServicio(new ProveedorFijo { Tasa = 60.5m });

        // Actuar
        var costo = await servicio.ConvertirAsync(new ActorTaller(_clienteId, false), orden.Id, " dop ");

        // Verificar
        Assert.Equal("USD", costo.MonedaOrigen);
        Assert.Equal(627m, costo.CostoOriginal);
        Assert.Equal("DOP", costo.MonedaDestino);
        Assert.Equal(60.5m, costo.Tasa);
        Assert.Equal(37933.50m, costo.CostoEnMonedaDestino);
        Assert.Equal(FechaTasa, costo.TasaActualizadaUtc);
    }

    [Fact]
    public async Task Convertir_RedondeaADosDecimales()
    {
        // Preparar
        var orden = await CotizarAsync();
        var servicio = NuevoServicio(new ProveedorFijo { Tasa = 0.12345m });

        // Actuar
        var costo = await servicio.ConvertirAsync(new ActorTaller(_clienteId, false), orden.Id, "EUR");

        // Verificar
        Assert.Equal(77.40m, costo.CostoEnMonedaDestino);
    }

    [Fact]
    public async Task Convertir_Administrador_ConvierteUnaOrdenAjena()
    {
        // Preparar
        var orden = await CotizarAsync();

        // Actuar
        var costo = await NuevoServicio(new ProveedorFijo()).ConvertirAsync(new ActorTaller(_administradorId, true), orden.Id, "DOP");

        // Verificar
        Assert.Equal(orden.Id, costo.OrdenId);
    }

    [Fact]
    public async Task Convertir_OrdenDeOtroCliente_SeRechazaSinConsultarLaApi()
    {
        // Preparar
        var orden = await CotizarAsync();
        var otroCliente = new ActorTaller(_bd.CrearUsuario(RolUsuario.Estandar), false);
        var proveedor = new ProveedorFijo();

        // Actuar
        var accion = () => NuevoServicio(proveedor).ConvertirAsync(otroCliente, orden.Id, "DOP");

        // Verificar
        await Assert.ThrowsAsync<OrdenNoEncontradaException>(accion);
        Assert.Equal(0, proveedor.Llamadas);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DO")]
    [InlineData("DOPP")]
    [InlineData("D0P")]
    [InlineData("../")]
    public async Task Convertir_MonedaInvalida_SeRechazaSinConsultarLaApi(string? moneda)
    {
        // Preparar
        var orden = await CotizarAsync();
        var proveedor = new ProveedorFijo();

        // Actuar
        var accion = () => NuevoServicio(proveedor).ConvertirAsync(new ActorTaller(_clienteId, false), orden.Id, moneda);

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
        Assert.Equal(0, proveedor.Llamadas);
    }

    [Fact]
    public async Task Convertir_ApiNoDisponible_SeRechazaYLaOrdenNoCambia()
    {
        // Preparar
        var orden = await CotizarAsync();
        var proveedor = new ProveedorFijo { Error = new TasaCambioNoDisponibleException("El servicio no responde.") };

        // Actuar
        var accion = () => NuevoServicio(proveedor).ConvertirAsync(new ActorTaller(_clienteId, false), orden.Id, "DOP");

        // Verificar
        await Assert.ThrowsAsync<TasaCambioNoDisponibleException>(accion);
        var guardada = await _bd.NuevoServicioOrdenes().ObtenerDelClienteAsync(_clienteId, orden.Id);
        Assert.Equal(orden.CostoTotal, guardada.CostoTotal);
        Assert.Equal("Cotizada", guardada.Estado);
    }
}
