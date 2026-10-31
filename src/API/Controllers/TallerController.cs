using API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCBuilderOps.Services;

namespace API.Controllers;

// RF-GP-05: el permiso OperarTaller lo exige IOrdenesService en cada operación.
[ApiController]
[Route("api/taller/ordenes")]
[Authorize(Roles = "Estandar,Administrador")]
public class TallerController : NegocioControllerBase
{
    private readonly IOrdenesService _ordenes;

    public TallerController(IOrdenesService ordenes)
    {
        _ordenes = ordenes;
    }

    // RF-GP-05
    [HttpGet]
    public Task<IActionResult> Listar(CancellationToken ct) =>
        Ejecutar(async () => Ok(await _ordenes.ListarParaTallerAsync(ActorActual(), ct)));

    // FN-04
    [HttpPost("{id:guid}/pasar-a-pruebas")]
    public Task<IActionResult> PasarAPruebas(Guid id, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _ordenes.PasarAPruebasAsync(ActorActual(), id, ct)));

    // FN-03
    [HttpPost("{id:guid}/pruebas")]
    public Task<IActionResult> RegistrarPrueba(Guid id, [FromBody] RegistrarPruebaRequest request, CancellationToken ct) =>
        Ejecutar(async () =>
        {
            var datos = new DatosPrueba(
                request.TipoSoftwarePrueba,
                request.TemperaturaMaximaCpu,
                request.TemperaturaMaximaGpu,
                request.PuntajeBenchmark!.Value,
                request.EsAprobada!.Value);

            var orden = await _ordenes.RegistrarPruebaAsync(ActorActual(), id, datos, ct);
            return StatusCode(StatusCodes.Status201Created, orden);
        });

    // FN-04
    [HttpPost("{id:guid}/entregar")]
    public Task<IActionResult> Entregar(Guid id, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _ordenes.EntregarAsync(ActorActual(), id, ct)));
}
