using System.Security.Claims;
using API.DTOs;
using Core.Auth.Exceptions;
using Core.Reportes.Domain;
using Core.Reportes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCBuilderOps.Services;

namespace API.Controllers;

// RF-REP-01 a RF-REP-05, RD-06
[ApiController]
[Route("api/reportes")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IReportesService _reportes;
    private readonly IReporteFabricantesService _reporteFabricantes;
    private readonly IReporteTiempoEnsambleService _reporteTiempoEnsamble;

    public ReportesController(
        IReportesService reportes,
        IReporteFabricantesService reporteFabricantes,
        IReporteTiempoEnsambleService reporteTiempoEnsamble)
    {
        _reportes = reportes;
        _reporteFabricantes = reporteFabricantes;
        _reporteTiempoEnsamble = reporteTiempoEnsamble;
    }

    // RF-REP-01
    [HttpGet("solicitudes")]
    public Task<IActionResult> Solicitudes([FromQuery] string? desde, [FromQuery] string? hasta, CancellationToken ct) =>
        Ejecutar(desde, hasta, async rango => Ok(await _reportes.SolicitudesPorEstadoAsync(rango, AlcanceDelUsuario(), ct)));

    // RF-REP-02, RF-NEG-08
    [HttpGet("fabricantes")]
    public Task<IActionResult> Fabricantes([FromQuery] string? desde, [FromQuery] string? hasta, CancellationToken ct) =>
        Ejecutar(desde, hasta, async rango => Ok(await _reporteFabricantes.FabricantesAsync(rango, AlcanceDelUsuario(), ct)));

    // RF-REP-02, RF-NEG-08, CA-HIS-14 a CA-HIS-16
    [HttpGet("tiempo-ensamble")]
    public Task<IActionResult> TiempoEnsamble([FromQuery] string? desde, [FromQuery] string? hasta, CancellationToken ct) =>
        Ejecutar(desde, hasta, async rango => Ok(await _reporteTiempoEnsamble.TiempoEnsambleAsync(rango, AlcanceDelUsuario(), ct)));

    private static async Task<IActionResult> Ejecutar(string? desde, string? hasta, Func<RangoFechas, Task<IActionResult>> reporte)
    {
        RangoFechas rango;

        try
        {
            rango = RangoFechas.Crear(desde, hasta);
        }
        catch (ValidacionException ex)
        {
            return new BadRequestObjectResult(new MensajeRespuesta(ex.Message));
        }

        return await reporte(rango);
    }

    // RF-REP-04: el Administrador ve todo; el Estándar, solo lo suyo.
    private Guid? AlcanceDelUsuario() =>
        User.IsInRole("Administrador") ? null : Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
