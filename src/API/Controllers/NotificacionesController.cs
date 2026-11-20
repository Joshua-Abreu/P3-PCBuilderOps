using System.Security.Claims;
using API.DTOs;
using Core.Notifications.Exceptions;
using Core.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// RF-NOT-04, RF-NOT-05, RF-NOT-07, RD-06
[ApiController]
[Route("api/notificaciones")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionesService _notificaciones;

    public NotificacionesController(INotificacionesService notificaciones)
    {
        _notificaciones = notificaciones;
    }

    // RF-NOT-04
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool soloNoLeidas, CancellationToken ct) =>
        Ok(await _notificaciones.ListarAsync(UsuarioActualId(), soloNoLeidas, ct));

    // RF-NOT-05
    [HttpPatch("{id:guid}/leida")]
    public async Task<IActionResult> MarcarLeida(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _notificaciones.MarcarLeidaAsync(UsuarioActualId(), id, ct));
        }
        catch (NotificacionNoEncontradaException ex)
        {
            return NotFound(new MensajeRespuesta(ex.Message));
        }
    }

    // RF-NOT-07
    [HttpGet("preferencias")]
    public async Task<IActionResult> ObtenerPreferencias(CancellationToken ct) =>
        Ok(await _notificaciones.ObtenerPreferenciasAsync(UsuarioActualId(), ct));

    // RF-NOT-07
    [HttpPut("preferencias")]
    public async Task<IActionResult> ActualizarPreferencias([FromBody] PreferenciasNotificacionRequest request, CancellationToken ct)
    {
        if (request.CorreoActivo is not bool correoActivo)
            return BadRequest(new MensajeRespuesta("Indica correoActivo con true o false."));

        return Ok(await _notificaciones.ActualizarPreferenciasAsync(UsuarioActualId(), correoActivo, ct));
    }

    private Guid UsuarioActualId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
