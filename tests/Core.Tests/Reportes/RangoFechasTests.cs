using Core.Auth.Exceptions;
using Core.Reportes.Domain;

namespace Core.Tests.Reportes;

// CA-REP-06 · RF-REP-05, RD-07
public sealed class RangoFechasTests
{
    [Fact]
    public void Crear_RangoValido_ConservaLasFechasYEmpiezaEnUtc()
    {
        // Actuar
        var rango = RangoFechas.Crear("2026-11-01", "2026-11-30");

        // Verificar
        Assert.Equal(new DateOnly(2026, 11, 1), rango.Desde);
        Assert.Equal(new DateOnly(2026, 11, 30), rango.Hasta);
        Assert.Equal(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), rango.DesdeUtc);
        Assert.Equal(DateTimeKind.Utc, rango.DesdeUtc.Kind);
    }

    [Fact]
    public void Crear_UnSoloDia_EsValido()
    {
        // Actuar
        var rango = RangoFechas.Crear("2026-11-19", "2026-11-19");

        // Verificar
        Assert.Equal(rango.Desde, rango.Hasta);
    }

    [Theory]
    [InlineData(null, "2026-11-30")]
    [InlineData("2026-11-01", null)]
    [InlineData("", "2026-11-30")]
    [InlineData("2026-11-01", "   ")]
    public void Crear_SinDesdeOSinHasta_SeRechaza(string? desde, string? hasta)
    {
        // Actuar
        var error = Assert.Throws<ValidacionException>(() => RangoFechas.Crear(desde, hasta));

        // Verificar
        Assert.Equal("Indica desde y hasta con el formato yyyy-MM-dd.", error.Message);
    }

    [Theory]
    [InlineData("2026-13-01", "2026-11-30", "desde")]
    [InlineData("01/11/2026", "2026-11-30", "desde")]
    [InlineData("2026-11-01", "2026-02-30", "hasta")]
    [InlineData("2026-11-01", "mañana", "hasta")]
    [InlineData("2026-11-01", "2026-11-30T00:00:00", "hasta")]
    public void Crear_FechaMalEscrita_SeRechazaNombrandoElCampo(string desde, string hasta, string campo)
    {
        // Actuar
        var error = Assert.Throws<ValidacionException>(() => RangoFechas.Crear(desde, hasta));

        // Verificar
        Assert.Equal($"La fecha {campo} no es válida. Usa el formato yyyy-MM-dd.", error.Message);
    }

    [Fact]
    public void Crear_DesdePosteriorAHasta_SeRechaza()
    {
        // Actuar
        var error = Assert.Throws<ValidacionException>(() => RangoFechas.Crear("2026-12-01", "2026-11-30"));

        // Verificar
        Assert.Equal("La fecha desde no puede ser posterior a hasta.", error.Message);
    }
}
