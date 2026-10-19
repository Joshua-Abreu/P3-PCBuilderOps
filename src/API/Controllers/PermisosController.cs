using System.Security.Claims;
using API.DTOs;
using Core.Auth.Exceptions;
using Core.Permisos.Domain;
using Core.Permisos.Exceptions;
using Core.Permisos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/permisos/solicitudes")]
[Authorize]
public class PermisosController : ControllerBase
{
    private readonly IGestionPermisosService _gestionPermisos;

    public PermisosController(IGestionPermisosService gestionPermisos)
    {
        _gestionPermisos = gestionPermisos;
    }

    // RF-GP-01
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearSolicitudPermisoRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoPermiso>(request.Permiso, true, out var permiso) || !Enum.IsDefined(permiso))
            return BadRequest(new MensajeRespuesta($"Permiso inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<TipoPermiso>())}."));

        return await Ejecutar(async () =>
        {
            var solicitud = await _gestionPermisos.CrearAsync(UsuarioActualId(), permiso, ct);
            return StatusCode(StatusCodes.Status201Created, solicitud);
        });
    }

    // RF-GP-02
    [HttpGet("mias")]
    public async Task<IActionResult> ListarMias(CancellationToken ct) =>
        Ok(await _gestionPermisos.ListarDelSolicitanteAsync(UsuarioActualId(), ct));

    // RF-GP-02
    [HttpGet("pendientes")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ListarPendientes(CancellationToken ct) =>
        Ok(await _gestionPermisos.ListarPendientesAsync(ct));

    // RF-GP-03, RF-GP-05, RF-GP-06, RF-GP-07
    [HttpPost("{id:guid}/aprobar")]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> Aprobar(Guid id, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _gestionPermisos.AprobarAsync(id, UsuarioActualId(), ct)));

    // RF-GP-04, RF-GP-06, RF-GP-07
    [HttpPost("{id:guid}/rechazar")]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> Rechazar(Guid id, [FromBody] RechazarSolicitudRequest request, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _gestionPermisos.RechazarAsync(id, UsuarioActualId(), request.Motivo, ct)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> operacion)
    {
        try
        {
            return await operacion();
        }
        catch (ValidacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
        catch (ResolucionPropiaException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MensajeRespuesta(ex.Message));
        }
        catch (SolicitudNoEncontradaException ex)
        {
            return NotFound(new MensajeRespuesta(ex.Message));
        }
        catch (TransicionNoPermitidaException ex)
        {
            return Conflict(new MensajeRespuesta(ex.Message));
        }
        catch (SolicitudDuplicadaException ex)
        {
            return Conflict(new MensajeRespuesta(ex.Message));
        }
    }

    private Guid UsuarioActualId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
