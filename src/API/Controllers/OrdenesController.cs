using API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCBuilderOps.Services;

namespace API.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "Estandar,Administrador")]
public class OrdenesController : NegocioControllerBase
{
    private readonly IOrdenesService _ordenes;

    public OrdenesController(IOrdenesService ordenes)
    {
        _ordenes = ordenes;
    }

    // FN-01
    [HttpGet("componentes")]
    public async Task<IActionResult> ListarComponentes(CancellationToken ct) =>
        Ok(await _ordenes.ListarComponentesAsync(ct));

    // FN-01, RF-NEG-06
    [HttpPost("ordenes")]
    [Authorize(Roles = "Estandar")]
    public Task<IActionResult> Cotizar([FromBody] CotizarOrdenRequest request, CancellationToken ct) =>
        Ejecutar(async () =>
        {
            if (request.Componentes.Any(l => l is null))
                return BadRequest(new MensajeRespuesta("La lista de componentes no puede incluir elementos vacíos."));

            var lineas = request.Componentes.Select(l => new LineaCotizacion(l.ComponenteId, l.Cantidad)).ToList();
            var orden = await _ordenes.CotizarAsync(UsuarioActualId(), lineas, ct);
            return StatusCode(StatusCodes.Status201Created, orden);
        });

    // FN-05
    [HttpGet("ordenes/mias")]
    public async Task<IActionResult> ListarMias(CancellationToken ct) =>
        Ok(await _ordenes.ListarDelClienteAsync(UsuarioActualId(), ct));

    // FN-05, RF-NEG-06
    [HttpGet("ordenes/{id:guid}")]
    public Task<IActionResult> ObtenerMia(Guid id, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _ordenes.ObtenerDelClienteAsync(UsuarioActualId(), id, ct)));

    // FN-02, RF-NEG-06
    [HttpPost("ordenes/{id:guid}/iniciar-armado")]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> IniciarArmado(Guid id, [FromBody] IniciarArmadoRequest request, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _ordenes.IniciarArmadoAsync(id, request.TecnicoId, ct)));
}
