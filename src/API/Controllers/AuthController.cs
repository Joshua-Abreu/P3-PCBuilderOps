using API.DTOs;
using Core.Auth.Exceptions;
using Core.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controlador de autenticación.
/// RD-02: Cero lógica de negocio. Solo recibe DTOs, valida ModelState y delega.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registra un nuevo usuario.
    /// RF-CA-01: Correo único. RF-CA-14: Política de contraseña. RF-CA-15: Activación por token.
    /// </summary>
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

    /// <summary>
    /// Activa la cuenta mediante token.
    /// RF-CA-16: Validación de token y activación.
    /// </summary>
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

    /// <summary>
    /// Reenvía el correo de activación.
    /// RF-CA-17: Respuesta siempre exitosa e idéntica exista o no el correo.
    /// </summary>
    [HttpPost("reenviar-activacion")]
    public async Task<IActionResult> ReenviarActivacion([FromBody] ReenviarActivacionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new MensajeRespuesta("Datos de entrada inválidos."));

        await _authService.ReenviarActivacionAsync(request.Correo, ct);

        // RF-CA-17: Siempre 200 OK con el mismo mensaje
        return Ok(new MensajeRespuesta("Si la cuenta existe y está pendiente de activación, se ha enviado un nuevo enlace"));
    }
}
