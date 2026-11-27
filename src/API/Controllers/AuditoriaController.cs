using System.Security.Claims;
using API.DTOs;
using Core.Auditoria.Exceptions;
using Core.Auditoria.Services;
using Core.Auth.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// RF-AUD-06, RF-AUD-07, RD-06
[ApiController]
[Route("api/auditoria")]
[Authorize(Roles = "Administrador")]
public class AuditoriaController : ControllerBase
{
    private readonly IHistorialAuditoria _historial;

    public AuditoriaController(IHistorialAuditoria historial)
    {
        _historial = historial;
    }

    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] string? usuarioId,
        [FromQuery] string? entidad,
        [FromQuery] string? desde,
        [FromQuery] string? hasta,
        [FromQuery] string? pagina,
        [FromQuery] string? tamanoPagina,
        CancellationToken ct)
    {
        try
        {
            var actor = new ActorAuditoria(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.IsInRole("Administrador"));
            var consulta = new ConsultaAuditoria(usuarioId, entidad, desde, hasta, pagina, tamanoPagina);
            return Ok(await _historial.ConsultarAsync(actor, consulta, ct));
        }
        catch (ValidacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
        catch (AccesoAuditoriaDenegadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MensajeRespuesta(ex.Message));
        }
    }
}
