using Core.Auth.Exceptions;
using Core.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly IPasswordService _passwordService;

    public UsuariosController(IPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    [HttpPost("{id:guid}/forzar-cambio-password")]
    public async Task<IActionResult> ForzarCambioPassword(Guid id, CancellationToken ct)
    {
        try
        {
            await _passwordService.ForzarCambioPasswordAsync(id, ct);
            return Ok(new { mensaje = "Se ha marcado el usuario para cambio de contraseña." });
        }
        catch (AutenticacionException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}
