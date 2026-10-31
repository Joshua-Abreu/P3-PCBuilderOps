using System.Security.Claims;
using API.DTOs;
using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Microsoft.AspNetCore.Mvc;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Services;

namespace API.Controllers;

// RD-07, RD-08
public abstract class NegocioControllerBase : ControllerBase
{
    protected Guid UsuarioActualId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected ActorTaller ActorActual() =>
        new(UsuarioActualId(), User.IsInRole(nameof(RolUsuario.Administrador)));

    protected async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> operacion)
    {
        try
        {
            return await operacion();
        }
        catch (ValidacionException ex)
        {
            return BadRequest(new MensajeRespuesta(ex.Message));
        }
        catch (PermisoTallerRequeridoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MensajeRespuesta(ex.Message));
        }
        catch (OrdenNoEncontradaException ex)
        {
            return NotFound(new MensajeRespuesta(ex.Message));
        }
        catch (TransicionOrdenNoPermitidaException ex)
        {
            return Conflict(new MensajeRespuesta(ex.Message));
        }
        catch (PruebaFueraDeEstadoException ex)
        {
            return Conflict(new MensajeRespuesta(ex.Message));
        }
        catch (PruebasNoAprobadasException ex)
        {
            return Conflict(new MensajeRespuesta(ex.Message));
        }
        catch (StockInsuficienteException ex)
        {
            return Conflict(new MensajeRespuesta(ex.Message));
        }
    }
}
