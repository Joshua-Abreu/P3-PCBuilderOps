using API.DTOs;
using Core.Auth.Exceptions;
using Core.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// RF-NOT-11, RD-06
[ApiController]
[Route("api/correos")]
[Authorize(Roles = "Administrador")]
public class CorreosController : ControllerBase
{
    private readonly IColaCorreoService _colaCorreoService;

    public CorreosController(IColaCorreoService colaCorreoService)
    {
        _colaCorreoService = colaCorreoService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? estado, [FromQuery] string? destinatario, CancellationToken ct)
    {
        try
        {
            return Ok(await _colaCorreoService.ListarAsync(estado, destinatario, ct));
        }
        catch (ValidacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
    }
}
