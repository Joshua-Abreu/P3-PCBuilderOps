using Core.Auth.Domain;
using Core.Reportes.Domain;
using PCBuilderOps.Data;
using PCBuilderOps.Domain;
using PCBuilderOps.Services;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Services;

// RF-REP-02, RF-REP-03, RF-REP-04, RF-REP-05, RF-NEG-08
public sealed class ReporteFabricantesServiceTests : IDisposable
{
    private static readonly Guid GpuNvidia = Guid.Parse("a3000000-0000-0000-0000-0000000000aa");
    private static readonly DateTime Noviembre = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Octubre = new(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc);

    private readonly BaseDeDatosDelNegocio _bd = new();
    private readonly Guid _cliente1;
    private readonly Guid _cliente2;

    public ReporteFabricantesServiceTests()
    {
        _cliente1 = _bd.CrearUsuario(RolUsuario.Estandar);
        _cliente2 = _bd.CrearUsuario(RolUsuario.Estandar);

        var contexto = _bd.NuevoContextoNegocio();
        contexto.Componentes.Add(Componente.Crear(
            GpuNvidia, "GeForce RTX 4070 Founders", CategoriaComponente.Gpu, "NVIDIA", "PCIe 4.0", 200, null, 599.00m, 5));
        contexto.SaveChanges();
    }

    public void Dispose() => _bd.Dispose();

    private static RangoFechas Rango(string desde, string hasta) => RangoFechas.Crear(desde, hasta);

    private ReporteFabricantesService NuevoServicio() => new(_bd.NuevoContextoNegocio());

    private void Guardar(Guid clienteId, DateTime fechaUtc, params (Guid ComponenteId, int Cantidad)[] lineas)
    {
        var contexto = _bd.NuevoContextoNegocio();
        var componentes = lineas
            .Select(l => (contexto.Componentes.Single(c => c.Id == l.ComponenteId), l.Cantidad))
            .ToList();

        contexto.OrdenesEnsamble.Add(OrdenEnsamble.Cotizar(clienteId, componentes, fechaUtc));
        contexto.SaveChanges();
    }

    private void GuardarTresOrdenesDeNoviembre()
    {
        Guardar(_cliente1, Noviembre, (ComponentesIniciales.CpuAm5, 1), (ComponentesIniciales.Ram16GbDdr4, 2));
        Guardar(_cliente2, Noviembre.AddDays(1), (ComponentesIniciales.CpuAm5, 1));
        Guardar(_cliente2, Noviembre.AddDays(2), (GpuNvidia, 1));
    }

    // CA-REP-02 · RF-REP-02, RF-REP-03
    [Fact]
    public async Task Fabricantes_AgrupaPiezasValorYOrdenesPorFabricante()
    {
        // Preparar
        GuardarTresOrdenesDeNoviembre();

        // Actuar
        var reporte = await NuevoServicio().FabricantesAsync(Rango("2026-11-01", "2026-11-30"), soloDelCliente: null);

        // Verificar
        Assert.Equal(
            new[]
            {
                new FilaFabricante("AMD", 2, 2 * 299.00m, 2),
                new FilaFabricante("Kingston", 2, 2 * 45.00m, 1),
                new FilaFabricante("NVIDIA", 1, 599.00m, 1)
            },
            reporte.Filas);
        Assert.Equal(5, reporte.TotalUnidades);
        Assert.Equal(598.00m + 90.00m + 599.00m, reporte.TotalValorUsd);
    }

    // CA-REP-03 · RF-REP-03
    [Fact]
    public async Task Fabricantes_LaAgrupacionYLasSumasSeHacenEnLaBaseDeDatos()
    {
        // Preparar
        GuardarTresOrdenesDeNoviembre();
        var sentencias = new List<string>();
        var servicio = new ReporteFabricantesService(_bd.NuevoContextoNegocioQueRegistraSql(sentencias));

        // Actuar
        await servicio.FabricantesAsync(Rango("2026-11-01", "2026-11-30"), soloDelCliente: null);

        // Verificar
        var consulta = Assert.Single(sentencias, s => s.Contains("DetallesEnsamble"));
        Assert.Contains("GROUP BY", consulta);
        Assert.Contains("COUNT(DISTINCT", consulta);
    }

    // CA-REP-04 · RF-REP-04
    [Fact]
    public async Task Fabricantes_DeUnCliente_SoloResumeSusOrdenes()
    {
        // Preparar
        GuardarTresOrdenesDeNoviembre();
        var rango = Rango("2026-01-01", "2026-12-31");

        // Actuar
        var delCliente1 = await NuevoServicio().FabricantesAsync(rango, _cliente1);
        var deTodos = await NuevoServicio().FabricantesAsync(rango, soloDelCliente: null);

        // Verificar
        Assert.Equal(
            new[] { new FilaFabricante("Kingston", 2, 90.00m, 1), new FilaFabricante("AMD", 1, 299.00m, 1) },
            delCliente1.Filas);
        Assert.Equal(5, deTodos.TotalUnidades);
    }

    // CA-REP-05 · RF-REP-05
    [Fact]
    public async Task Fabricantes_UnRangoQueTerminaAntes_NoDevuelveFilas()
    {
        // Preparar
        GuardarTresOrdenesDeNoviembre();

        // Actuar
        var reporte = await NuevoServicio().FabricantesAsync(Rango("2026-11-01", "2026-11-09"), soloDelCliente: null);

        // Verificar
        Assert.Empty(reporte.Filas);
        Assert.Equal(0, reporte.TotalUnidades);
        Assert.Equal(0m, reporte.TotalValorUsd);
    }

    // CA-REP-05 · RF-REP-05
    [Fact]
    public async Task Fabricantes_DosMesesDistintos_DanResultadosDistintosYNoVacios()
    {
        // Preparar
        GuardarTresOrdenesDeNoviembre();
        Guardar(_cliente1, Octubre, (ComponentesIniciales.Ram16GbDdr4, 4));

        // Actuar
        var octubre = await NuevoServicio().FabricantesAsync(Rango("2026-10-01", "2026-10-31"), soloDelCliente: null);
        var noviembre = await NuevoServicio().FabricantesAsync(Rango("2026-11-01", "2026-11-30"), soloDelCliente: null);

        // Verificar
        Assert.Equal(new[] { new FilaFabricante("Kingston", 4, 180.00m, 1) }, octubre.Filas);
        Assert.Equal(new[] { "AMD", "Kingston", "NVIDIA" }, noviembre.Filas.Select(f => f.Fabricante));
    }

    // Hallazgo 3 (Asignación 3) · RF-REP-05, RD-07
    [Fact]
    public async Task Fabricantes_ConLasFechasDeLosBordesDelCalendario_Responde()
    {
        // Preparar
        GuardarTresOrdenesDeNoviembre();

        // Actuar
        var todo = await NuevoServicio().FabricantesAsync(Rango("0001-01-01", "9999-12-31"), soloDelCliente: null);
        var ultimoDia = await NuevoServicio().FabricantesAsync(Rango("9999-12-31", "9999-12-31"), soloDelCliente: null);

        // Verificar
        Assert.Equal(5, todo.TotalUnidades);
        Assert.Empty(ultimoDia.Filas);
    }
}
