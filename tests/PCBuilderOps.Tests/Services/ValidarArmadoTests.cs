using Core.Auth.Exceptions;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// FN-01
public sealed class ValidarArmadoTests : IDisposable
{
    private readonly BaseDeDatosDelNegocio _bd = new();

    public void Dispose() => _bd.Dispose();

    private Task<ValidacionArmado> ValidarAsync(params LineaCotizacion[] lineas) =>
        _bd.NuevoServicioOrdenes().ValidarArmadoAsync(lineas);

    [Fact]
    public async Task Validar_ArmadoCompatible_DevuelveConsumoPotenciaYTotal()
    {
        // Actuar
        var resultado = await ValidarAsync(
            new(ComponentesIniciales.CpuAm5, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.GpuMedia, 1),
            new(ComponentesIniciales.Fuente850W, 1));

        // Verificar
        Assert.True(resultado.EsCompatible);
        Assert.Empty(resultado.Faltantes);
        Assert.Empty(resultado.Problemas);
        Assert.Equal(260, resultado.ConsumoWatts);
        Assert.Equal(850, resultado.PotenciaWatts);
        Assert.Equal(926m, resultado.Total);
    }

    [Fact]
    public async Task Validar_SocketYPotenciaIncompatibles_InformaAmbosProblemas()
    {
        // Actuar
        var resultado = await ValidarAsync(
            new(ComponentesIniciales.CpuLga1700, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.GpuAlta, 1),
            new(ComponentesIniciales.Fuente450W, 1));

        // Verificar
        Assert.False(resultado.EsCompatible);
        Assert.Equal(
            ["La CPU usa LGA1700 y la placa base usa AM5.", "La fuente entrega 450 W y los componentes consumen 615 W."],
            resultado.Problemas);
    }

    [Fact]
    public async Task Validar_ArmadoVacio_IndicaLosObligatoriosQueFaltan()
    {
        // Actuar
        var resultado = await ValidarAsync();

        // Verificar
        Assert.False(resultado.EsCompatible);
        Assert.Equal(["Cpu", "PlacaBase", "Fuente"], resultado.Faltantes);
        Assert.Empty(resultado.Problemas);
        Assert.Equal(0m, resultado.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public async Task Validar_CantidadFueraDeRango_LaInformaComoProblema(int cantidad)
    {
        // Actuar
        var resultado = await ValidarAsync(
            new(ComponentesIniciales.CpuAm5, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.Ram32Gb, cantidad),
            new(ComponentesIniciales.Fuente850W, 1));

        // Verificar
        Assert.False(resultado.EsCompatible);
        Assert.Contains("La cantidad de cada componente debe estar entre 1 y 16.", resultado.Problemas);
    }

    [Fact]
    public async Task Validar_NoCreaOrdenesNiReservaExistencias()
    {
        // Actuar
        await ValidarAsync(
            new(ComponentesIniciales.CpuAm5, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.GpuAlta, 1),
            new(ComponentesIniciales.Fuente850W, 1));

        // Verificar
        await using var contexto = _bd.NuevoContextoNegocio();
        Assert.Equal(0, await contexto.OrdenesEnsamble.CountAsync());
        Assert.Equal(1, (await contexto.Componentes.SingleAsync(c => c.Id == ComponentesIniciales.GpuAlta)).StockDisponible);
    }

    [Fact]
    public async Task Validar_ComponenteInexistente_SeRechaza()
    {
        // Actuar
        var accion = () => ValidarAsync(new LineaCotizacion(Guid.NewGuid(), 1));

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
    }

    [Fact]
    public async Task Validar_ComponenteRepetido_SeRechaza()
    {
        // Actuar
        var accion = () => ValidarAsync(
            new(ComponentesIniciales.CpuAm5, 1),
            new(ComponentesIniciales.CpuAm5, 1));

        // Verificar
        await Assert.ThrowsAsync<ValidacionException>(accion);
    }
}
