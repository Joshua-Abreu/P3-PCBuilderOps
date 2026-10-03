using Core.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Administrador")]
public class AdminController : ControllerBase
{
    private readonly IColaCorreoService _colaCorreoService;

    public AdminController(IColaCorreoService colaCorreoService)
    {
        _colaCorreoService = colaCorreoService;
    }

    [HttpPost("correos/despachar")]
    public async Task<IActionResult> DespacharCorreos(CancellationToken ct)
    {
        var enviados = await _colaCorreoService.DespacharPendientesAsync(ct);
        return Ok(new { mensaje = $"Se enviaron {enviados} correos pendientes.", enviados });
    }
}
