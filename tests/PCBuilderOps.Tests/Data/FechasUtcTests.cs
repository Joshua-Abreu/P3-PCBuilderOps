using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Data;

// Hallazgo 4 (Asignación 3) · RD-11: todas las fechas del módulo se leen de la base como UTC.
public sealed class FechasUtcTests : IDisposable
{
    private readonly BaseDeDatosDelNegocio _bd = new();

    public void Dispose() => _bd.Dispose();

    // Hallazgo 4
    [Fact]
    public void TodaPropiedadDeFechaDelModulo_SeLeeDeLaBaseComoUtc()
    {
        // Preparar
        var propiedades = _bd.NuevoContextoNegocio().Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
            .ToList();
        var leidaSinZona = new DateTime(2026, 11, 26, 22, 11, 15, DateTimeKind.Unspecified);

        // Actuar
        var sinUtc = propiedades
            .Where(p => p.GetValueConverter()?.ConvertFromProvider(leidaSinZona) is not DateTime { Kind: DateTimeKind.Utc })
            .Select(p => $"{p.DeclaringType.ClrType.Name}.{p.Name}")
            .ToList();

        // Verificar
        Assert.NotEmpty(propiedades);
        Assert.Empty(sinUtc);
    }

    // Hallazgo 4
    [Fact]
    public async Task OrdenYPrueba_LeidasDeLaBase_SonUtc()
    {
        // Preparar
        var clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(clienteId,
        [
            new(ComponentesIniciales.CpuAm5, 1),
            new(ComponentesIniciales.PlacaAm5, 1),
            new(ComponentesIniciales.GpuMedia, 1),
            new(ComponentesIniciales.Fuente850W, 1)
        ]);

        // Actuar
        var leida = await _bd.NuevoServicioOrdenes().ObtenerDelClienteAsync(clienteId, orden.Id);
        var guardada = await _bd.NuevoContextoNegocio().OrdenesEnsamble.AsNoTracking().SingleAsync(o => o.Id == orden.Id);

        // Verificar
        Assert.Equal(DateTimeKind.Utc, leida.FechaCreacionUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, guardada.FechaCreacionUtc.Kind);
    }
}
