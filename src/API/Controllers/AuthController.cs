using API.DTOs;
using Core.Auth.Exceptions;
using Core.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Registrar([FromBody] RegistroRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new MensajeRespuesta("Datos de entrada inválidos."));

        try
        {
            await _authService.RegistrarAsync(request.Correo, request.Password, ct);
            return StatusCode(201, new MensajeRespuesta("Registro exitoso. Revisa tu correo para activar la cuenta."));
        }
        catch (ValidacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
        catch (AutenticacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
    }

    [HttpGet("activar")]
    public async Task<IActionResult> Activar([FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new MensajeRespuesta("El token es obligatorio."));

        try
        {
            await _authService.ActivarCuentaAsync(token, ct);
            return Ok(new MensajeRespuesta("Cuenta activada exitosamente."));
        }
        catch (ValidacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
        catch (AutenticacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
    }

    [HttpPost("reenviar-activacion")]
    public async Task<IActionResult> ReenviarActivacion([FromBody] ReenviarActivacionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new MensajeRespuesta("Datos de entrada inválidos."));

        await _authService.ReenviarActivacionAsync(request.Correo, ct);

        return Ok(new MensajeRespuesta("Si la cuenta existe y está pendiente de activación, se ha enviado un nuevo enlace"));
    }
}
