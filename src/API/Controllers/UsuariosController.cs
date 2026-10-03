using API.DTOs;
using Core.Auth.Domain;
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
    private readonly IUsuarioAdminService _usuarioAdminService;

    public UsuariosController(IUsuarioAdminService usuarioAdminService)
    {
        _usuarioAdminService = usuarioAdminService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10,
        [FromQuery] string? rol = null,
        [FromQuery] bool? activo = null,
        CancellationToken ct = default)
    {
        RolUsuario? rolFiltro = null;

        if (!string.IsNullOrWhiteSpace(rol) && Enum.TryParse<RolUsuario>(rol, true, out var parsedRol))
            rolFiltro = parsedRol;

        var resultado = await _usuarioAdminService.ListarAsync(pagina, tamanoPagina, rolFiltro, activo, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id, CancellationToken ct)
    {
        var usuario = await _usuarioAdminService.ObtenerPorIdAsync(id, ct);

        if (usuario is null)
            return NotFound(new MensajeRespuesta("Usuario no encontrado."));

        return Ok(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new MensajeRespuesta("Datos de entrada inválidos."));

        if (!Enum.TryParse<Core.Auth.Domain.RolUsuario>(request.Rol, true, out var rol))
            return BadRequest(new MensajeRespuesta("Rol inválido."));

        try
        {
            var usuario = await _usuarioAdminService.CrearAsync(
                request.Correo,
                request.Password,
                rol,
                request.ActivarInmediatamente,
                ct);

            return StatusCode(201, usuario);
        }
        catch (ValidacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
        catch (ReglaNegocioException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
    }

    [HttpPut("{id:guid}/rol")]
    public async Task<IActionResult> CambiarRol(Guid id, [FromBody] CambiarRolRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new MensajeRespuesta("Datos de entrada inválidos."));

        if (!Enum.TryParse<Core.Auth.Domain.RolUsuario>(request.Rol, true, out var rol))
            return BadRequest(new MensajeRespuesta("Rol inválido."));

        try
        {
            var adminId = ObtenerAdminId();
            await _usuarioAdminService.CambiarRolAsync(id, rol, adminId, ct);
            return Ok(new MensajeRespuesta("Rol actualizado exitosamente."));
        }
        catch (AutenticacionException ex)
        {
            return NotFound(new MensajeRespuesta(ex.Message));
        }
        catch (ReglaNegocioException ex)
        {
            return StatusCode(403, new MensajeRespuesta(ex.Message));
        }
    }

    [HttpPatch("{id:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new MensajeRespuesta("Datos de entrada inválidos."));

        try
        {
            var adminId = ObtenerAdminId();
            await _usuarioAdminService.CambiarEstadoAsync(id, request.Activar, adminId, ct);
            return Ok(new MensajeRespuesta(request.Activar ? "Usuario activado exitosamente." : "Usuario desactivado exitosamente."));
        }
        catch (AutenticacionException ex)
        {
            return NotFound(new MensajeRespuesta(ex.Message));
        }
        catch (ReglaNegocioException ex)
        {
            return StatusCode(403, new MensajeRespuesta(ex.Message));
        }
    }

    private Guid ObtenerAdminId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
