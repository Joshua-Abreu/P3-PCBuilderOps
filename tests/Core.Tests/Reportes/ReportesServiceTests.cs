using Core.Auth.Domain;
using Core.Permisos.Domain;
using Core.Reportes.Domain;
using Core.Reportes.Services;
using Core.Tests.Soporte;

namespace Core.Tests.Reportes;

// RF-REP-01, RF-REP-03, RF-REP-04, RF-REP-05
public sealed class ReportesServiceTests : IDisposable
{
    private static readonly DateTime Noviembre = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Octubre = new(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc);

    private readonly BaseDeDatosEnMemoria _baseDeDatos = new();
    private readonly Guid _administradorId;
    private readonly Guid _estandar1;
    private readonly Guid _estandar2;

    public ReportesServiceTests()
    {
        _administradorId = _baseDeDatos.CrearUsuario(RolUsuario.Administrador);
        _estandar1 = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
        _estandar2 = _baseDeDatos.CrearUsuario(RolUsuario.Estandar);
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private static RangoFechas Rango(string desde, string hasta) => RangoFechas.Crear(desde, hasta);

    private ReportesService NuevoServicio() => new(_baseDeDatos.NuevoContexto());

    private void Guardar(Guid solicitanteId, DateTime fechaUtc, EstadoSolicitud estado)
    {
        var solicitud = SolicitudPermiso.Crear(solicitanteId, TipoPermiso.OperarTaller, fechaUtc);

        if (estado == EstadoSolicitud.Aplicada)
        {
            solicitud.Aprobar(_administradorId, fechaUtc.AddHours(1));
            solicitud.Aplicar();
        }
        else if (estado == EstadoSolicitud.Rechazada)
        {
            solicitud.Rechazar(_administradorId, "Sin cupo", fechaUtc.AddHours(1));
        }

        using var contexto = _baseDeDatos.NuevoContexto();
        contexto.SolicitudesPermiso.Add(solicitud);
        contexto.SaveChanges();
    }

    private void GuardarCincoDeNoviembre()
    {
        Guardar(_estandar1, Noviembre, EstadoSolicitud.Pendiente);
        Guardar(_estandar2, Noviembre.AddDays(1), EstadoSolicitud.Pendiente);
        Guardar(_estandar1, Noviembre.AddDays(2), EstadoSolicitud.Aplicada);
        Guardar(_estandar2, Noviembre.AddDays(3), EstadoSolicitud.Aplicada);
        Guardar(_estandar2, Noviembre.AddDays(4), EstadoSolicitud.Rechazada);
    }

    // CA-REP-01 · RF-REP-01, RF-REP-03
    [Fact]
    public async Task Solicitudes_AgrupaPorMesYEstadoEnMenosFilasQueLasSolicitudes()
    {
        // Preparar
        GuardarCincoDeNoviembre();

        // Actuar
        var reporte = await NuevoServicio().SolicitudesPorEstadoAsync(Rango("2026-11-01", "2026-11-30"), soloDelSolicitante: null);

        // Verificar
        Assert.Equal(5, reporte.Total);
        Assert.Equal(
            new[]
            {
                new FilaSolicitudes("2026-11", "Pendiente", 2),
                new FilaSolicitudes("2026-11", "Aplicada", 2),
                new FilaSolicitudes("2026-11", "Rechazada", 1)
            },
            reporte.Filas);
    }

    // CA-REP-03 · RF-REP-03
    [Fact]
    public async Task Solicitudes_LaAgrupacionSeHaceEnLaBaseDeDatos()
    {
        // Preparar
        GuardarCincoDeNoviembre();
        var sentencias = new List<string>();
        var servicio = new ReportesService(_baseDeDatos.NuevoContextoQueRegistraSql(sentencias));

        // Actuar
        await servicio.SolicitudesPorEstadoAsync(Rango("2026-11-01", "2026-11-30"), soloDelSolicitante: null);

        // Verificar
        var consulta = Assert.Single(sentencias, s => s.Contains("SolicitudesPermiso"));
        Assert.Contains("GROUP BY", consulta);
        Assert.Contains("COUNT(", consulta);
    }

    // CA-REP-04 · RF-REP-04
    [Fact]
    public async Task Solicitudes_DeUnEstandar_SoloCuentaLasSuyas()
    {
        // Preparar
        GuardarCincoDeNoviembre();
        var rango = Rango("2026-01-01", "2026-12-31");

        // Actuar
        var delEstandar1 = await NuevoServicio().SolicitudesPorEstadoAsync(rango, _estandar1);
        var delEstandar2 = await NuevoServicio().SolicitudesPorEstadoAsync(rango, _estandar2);
        var deTodos = await NuevoServicio().SolicitudesPorEstadoAsync(rango, soloDelSolicitante: null);

        // Verificar
        Assert.Equal(2, delEstandar1.Total);
        Assert.Equal(3, delEstandar2.Total);
        Assert.Equal(5, deTodos.Total);
    }

    // CA-REP-05 · RF-REP-05
    [Fact]
    public async Task Solicitudes_UnRangoQueTerminaAntes_NoDevuelveFilas()
    {
        // Preparar
        GuardarCincoDeNoviembre();

        // Actuar
        var hastaElDiaAnterior = await NuevoServicio().SolicitudesPorEstadoAsync(Rango("2026-11-01", "2026-11-09"), soloDelSolicitante: null);

        // Verificar
        Assert.Empty(hastaElDiaAnterior.Filas);
        Assert.Equal(0, hastaElDiaAnterior.Total);
    }

    // CA-REP-05 · RF-REP-05
    [Fact]
    public async Task Solicitudes_DosMesesDistintos_DanResultadosDistintosYNoVacios()
    {
        // Preparar
        GuardarCincoDeNoviembre();
        Guardar(_estandar1, Octubre, EstadoSolicitud.Rechazada);

        // Actuar
        var octubre = await NuevoServicio().SolicitudesPorEstadoAsync(Rango("2026-10-01", "2026-10-31"), soloDelSolicitante: null);
        var noviembre = await NuevoServicio().SolicitudesPorEstadoAsync(Rango("2026-11-01", "2026-11-30"), soloDelSolicitante: null);
        var ambos = await NuevoServicio().SolicitudesPorEstadoAsync(Rango("2026-10-01", "2026-11-30"), soloDelSolicitante: null);

        // Verificar
        Assert.Equal(new[] { new FilaSolicitudes("2026-10", "Rechazada", 1) }, octubre.Filas);
        Assert.Equal(5, noviembre.Total);
        Assert.Equal(6, ambos.Total);
        Assert.Equal(new[] { "2026-10", "2026-11", "2026-11", "2026-11" }, ambos.Filas.Select(f => f.Periodo));
    }

    // CA-REP-05 · RF-REP-05, RD-11
    [Fact]
    public async Task Solicitudes_HastaIncluyeElDiaCompletoYNadaDelSiguiente()
    {
        // Preparar
        Guardar(_estandar1, new DateTime(2026, 11, 30, 23, 59, 59, DateTimeKind.Utc), EstadoSolicitud.Pendiente);
        Guardar(_estandar2, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), EstadoSolicitud.Pendiente);

        // Actuar
        var reporte = await NuevoServicio().SolicitudesPorEstadoAsync(Rango("2026-11-30", "2026-11-30"), soloDelSolicitante: null);

        // Verificar
        Assert.Equal(1, reporte.Total);
    }

    // Hallazgo 3 (Asignación 3) · RF-REP-05, RD-07
    [Fact]
    public async Task Solicitudes_ConLasFechasDeLosBordesDelCalendario_Responde()
    {
        // Preparar
        GuardarCincoDeNoviembre();
        Guardar(_estandar1, new DateTime(9999, 12, 31, 20, 0, 0, DateTimeKind.Utc), EstadoSolicitud.Rechazada);

        // Actuar
        var todo = await NuevoServicio().SolicitudesPorEstadoAsync(Rango("0001-01-01", "9999-12-31"), soloDelSolicitante: null);
        var ultimoDia = await NuevoServicio().SolicitudesPorEstadoAsync(Rango("9999-12-31", "9999-12-31"), soloDelSolicitante: null);

        // Verificar
        Assert.Equal(6, todo.Total);
        Assert.Equal(new[] { new FilaSolicitudes("9999-12", "Rechazada", 1) }, ultimoDia.Filas);
    }
}
