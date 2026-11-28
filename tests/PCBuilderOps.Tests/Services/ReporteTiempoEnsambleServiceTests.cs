using Core.Auth.Domain;
using Core.Reportes.Domain;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Domain;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// RF-REP-02 a RF-REP-05, RF-NEG-08 · criterios CA-HIS-14 a CA-HIS-17
public sealed class ReporteTiempoEnsambleServiceTests : IDisposable
{
    private static readonly LineaCotizacion[] PcCompatible =
    [
        new(ComponentesIniciales.CpuAm5, 1),
        new(ComponentesIniciales.PlacaAm5, 1),
        new(ComponentesIniciales.Fuente850W, 1)
    ];

    private readonly BaseDeDatosDelNegocio _bd = new();
    private readonly Guid _administradorId;
    private readonly Guid _clienteId;
    private readonly Guid _tecnico1;
    private readonly Guid _tecnico2;

    public ReporteTiempoEnsambleServiceTests()
    {
        _administradorId = _bd.CrearUsuario(RolUsuario.Administrador);
        _clienteId = _bd.CrearUsuario(RolUsuario.Estandar);
        _tecnico1 = _bd.CrearTecnicoConPermisoAsync(_administradorId).GetAwaiter().GetResult();
        _tecnico2 = _bd.CrearTecnicoConPermisoAsync(_administradorId).GetAwaiter().GetResult();
    }

    public void Dispose() => _bd.Dispose();

    private static RangoFechas Rango(string desde, string hasta) => RangoFechas.Crear(desde, hasta);

    private async Task<Guid> ArmarAsync(Guid tecnicoId, TimeSpan duracion, bool perderInicioDelArmado = false)
    {
        var orden = await _bd.NuevoServicioOrdenes().CotizarAsync(_clienteId, PcCompatible);
        await _bd.NuevoServicioOrdenes().IniciarArmadoAsync(_administradorId, orden.Id, tecnicoId);

        if (perderInicioDelArmado)
            await _bd.NuevoContextoNegocio().TransicionesOrden
                .Where(t => t.OrdenEnsambleId == orden.Id && t.EstadoNuevo == EstadoOrdenEnsamble.EnArmado)
                .ExecuteDeleteAsync();

        _bd.Reloj.Avanzar(duracion);
        await _bd.NuevoServicioOrdenes().PasarAPruebasAsync(new ActorTaller(tecnicoId, false), orden.Id);

        return orden.Id;
    }

    private async Task ArmarLasTresOrdenesDelCriterioAsync()
    {
        await ArmarAsync(_tecnico1, TimeSpan.FromHours(2));
        await ArmarAsync(_tecnico1, TimeSpan.FromHours(4));
        await ArmarAsync(_tecnico2, TimeSpan.FromHours(1));
    }

    // CA-HIS-14
    [Fact]
    public async Task TiempoEnsamble_PromedioMinimoYMaximoPorTecnico()
    {
        // Preparar
        await ArmarLasTresOrdenesDelCriterioAsync();

        // Actuar
        var reporte = await _bd.NuevoReporteTiempoEnsamble().TiempoEnsambleAsync(Rango("2026-10-31", "2026-10-31"), soloDelTecnico: null);

        // Verificar
        Assert.Equal(
            new[]
            {
                new FilaTecnico(_tecnico1, "Usuario de prueba", 2, 180m, 120m, 240m),
                new FilaTecnico(_tecnico2, "Usuario de prueba", 1, 60m, 60m, 60m)
            },
            reporte.Filas);
        Assert.Equal(3, reporte.TotalOrdenes);
        Assert.Equal(140m, reporte.PromedioGeneralMinutos);
        Assert.Equal(new DateOnly(2026, 10, 31), reporte.Desde);
    }

    // CA-HIS-14 · RF-REP-03
    [Fact]
    public async Task TiempoEnsamble_LaAgrupacionSeHaceEnLaBaseDeDatos()
    {
        // Preparar
        await ArmarLasTresOrdenesDelCriterioAsync();
        var sentencias = new List<string>();

        // Actuar
        await _bd.NuevoReporteTiempoEnsamble(_bd.NuevoContextoNegocioQueRegistraSql(sentencias))
            .TiempoEnsambleAsync(Rango("2026-10-31", "2026-10-31"), soloDelTecnico: null);

        // Verificar
        var consulta = Assert.Single(sentencias, s => s.Contains("TransicionesOrden"));
        Assert.Contains("GROUP BY", consulta);
        Assert.Contains("COUNT(", consulta);
        Assert.Contains("SUM(", consulta);
    }

    // CA-HIS-15 · RF-REP-05
    [Fact]
    public async Task TiempoEnsamble_ElRangoUsaElDiaEnQueTerminoElArmadoEIncluyeElUltimoDia()
    {
        // Preparar
        await ArmarAsync(_tecnico1, TimeSpan.FromHours(2));
        _bd.Reloj.Avanzar(TimeSpan.FromDays(1));
        await ArmarAsync(_tecnico1, TimeSpan.FromHours(4));
        var servicio = _bd.NuevoReporteTiempoEnsamble();

        // Actuar
        var primerDia = await servicio.TiempoEnsambleAsync(Rango("2026-10-31", "2026-10-31"), null);
        var segundoDia = await servicio.TiempoEnsambleAsync(Rango("2026-11-01", "2026-11-01"), null);
        var ambos = await servicio.TiempoEnsambleAsync(Rango("2026-10-31", "2026-11-01"), null);
        var bordes = await servicio.TiempoEnsambleAsync(Rango("0001-01-01", "9999-12-31"), null);

        // Verificar
        Assert.Equal(120m, Assert.Single(primerDia.Filas).PromedioMinutos);
        Assert.Equal(240m, Assert.Single(segundoDia.Filas).PromedioMinutos);
        Assert.Equal(2, ambos.TotalOrdenes);
        Assert.Equal(2, bordes.TotalOrdenes);
    }

    // CA-HIS-16 · RF-REP-04
    [Fact]
    public async Task TiempoEnsamble_UnTecnicoSoloVeSuFilaYUnClienteNoVeNinguna()
    {
        // Preparar
        await ArmarLasTresOrdenesDelCriterioAsync();
        var servicio = _bd.NuevoReporteTiempoEnsamble();
        var rango = Rango("2026-10-31", "2026-10-31");

        // Actuar
        var delTecnico = await servicio.TiempoEnsambleAsync(rango, soloDelTecnico: _tecnico1);
        var delCliente = await servicio.TiempoEnsambleAsync(rango, soloDelTecnico: _clienteId);

        // Verificar
        Assert.Equal(_tecnico1, Assert.Single(delTecnico.Filas).TecnicoId);
        Assert.Equal(2, delTecnico.TotalOrdenes);
        Assert.Empty(delCliente.Filas);
        Assert.Equal(0, delCliente.TotalOrdenes);
        Assert.Null(delCliente.PromedioGeneralMinutos);
    }

    // CA-HIS-17
    [Fact]
    public async Task TiempoEnsamble_UnaOrdenSinInicioDelArmadoRegistrado_NoCuenta()
    {
        // Preparar
        var sinInicio = await ArmarAsync(_tecnico1, TimeSpan.FromHours(3), perderInicioDelArmado: true);
        await ArmarAsync(_tecnico1, TimeSpan.FromHours(1));

        // Actuar
        var reporte = await _bd.NuevoReporteTiempoEnsamble().TiempoEnsambleAsync(Rango("2026-10-31", "2026-10-31"), null);

        // Verificar
        var fila = Assert.Single(reporte.Filas);
        Assert.Equal(1, fila.Ordenes);
        Assert.Equal(60m, fila.PromedioMinutos);
        var paso = Assert.Single(_bd.NuevoContextoNegocio().TransicionesOrden.AsNoTracking(),
            t => t.OrdenEnsambleId == sinInicio && t.EstadoNuevo == EstadoOrdenEnsamble.EnPruebas);
        Assert.Null(paso.SegundosEnEstadoAnterior);
    }
}
