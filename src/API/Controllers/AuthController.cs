using API.DTOs;
using Core.Auth.Exceptions;
using Core.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordService _passwordService;

    public AuthController(IAuthService authService, IPasswordService passwordService)
    {
        _authService = authService;
        _passwordService = passwordService;
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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new MensajeRespuesta("Datos de entrada inválidos."));

        try
        {
            var result = await _authService.LoginAsync(request.Correo, request.Password, ct);
            return Ok(new LoginRespuesta(result.Token, result.ExpiracionUtc, result.DebeCambiarPassword));
        }
        catch (ValidacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
        catch (AutenticacionException ex)
        {
            return Unauthorized(new MensajeRespuesta(ex.Message));
        }
    }

    [Authorize]
    [HttpGet("yo")]
    public async Task<IActionResult> Yo(CancellationToken ct)
    {
        var usuario = await _authService.ObtenerUsuarioAutenticadoAsync(ct);

        if (usuario is null)
            return Unauthorized(new MensajeRespuesta("No autenticado."));

        return Ok(new UsuarioAutenticadoRespuesta(usuario.Id, usuario.Correo, usuario.Rol.ToString()));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var authHeader = Request.Headers.Authorization.ToString();
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..].Trim()
            : string.Empty;

        await _authService.LogoutAsync(token, ct);

        return Ok(new MensajeRespuesta("Sesión cerrada exitosamente."));
    }

    [HttpPost("olvide-password")]
    public async Task<IActionResult> OlvidePassword([FromBody] OlvidePasswordRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new MensajeRespuesta("Datos de entrada inválidos."));

        await _passwordService.OlvidePasswordAsync(request.Correo, ct);

        return Ok(new MensajeRespuesta("Si el correo existe en el sistema, se ha enviado un enlace de recuperación."));
    }

    [HttpPost("restablecer-password")]
    public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new MensajeRespuesta("Datos de entrada inválidos."));

        try
        {
            await _passwordService.RestablecerPasswordAsync(request.Token, request.NuevaPassword, ct);
            return Ok(new MensajeRespuesta("Contraseña restablecida exitosamente."));
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
}
