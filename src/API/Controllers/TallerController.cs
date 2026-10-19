using API.Autorizacion;
using Core.Ordenes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/taller")]
[Authorize(Policy = PoliticasPermiso.OperarTaller)]
public class TallerController : ControllerBase
{
    private readonly IConsultaOrdenesService _consultaOrdenes;

    public TallerController(IConsultaOrdenesService consultaOrdenes)
    {
        _consultaOrdenes = consultaOrdenes;
    }

    // RF-GP-05
    [HttpGet("ordenes")]
    public async Task<IActionResult> ListarOrdenes(CancellationToken ct) =>
        Ok(await _consultaOrdenes.ListarAsync(ct));
}
