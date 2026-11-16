using API.DTOs;
using Core.Auth.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCBuilderOps.Domain;
using PCBuilderOps.Services;

namespace API.Controllers;

[ApiController]
[Route("api/componentes")]
[Authorize(Roles = "Estandar,Administrador")]
public class ComponentesController : NegocioControllerBase
{
    private const long LimiteSolicitudBytes = CatalogoService.TamanoMaximoImagenBytes + 1024 * 1024;

    private readonly ICatalogoService _catalogo;

    public ComponentesController(ICatalogoService catalogo)
    {
        _catalogo = catalogo;
    }

    // FN-01, RF-NEG-06
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _catalogo.ListarAsync(User.IsInRole(nameof(RolUsuario.Administrador)), ct));

    // RF-NEG-01, RD-06, RD-07
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> Crear([FromBody] GuardarComponenteRequest request, CancellationToken ct) =>
        Ejecutar(async () =>
            StatusCode(StatusCodes.Status201Created, await _catalogo.CrearAsync(ADatos(request), ct)));

    // RF-NEG-01, RD-06, RD-07
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> Actualizar(Guid id, [FromBody] GuardarComponenteRequest request, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _catalogo.ActualizarAsync(id, ADatos(request), ct)));

    // RF-NEG-01, RD-06
    [HttpPatch("{id:guid}/estado")]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoComponenteRequest request, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _catalogo.CambiarEstadoAsync(id, request.EstaActivo!.Value, ct)));

    // RF-DOC-01, RD-06, RD-07
    [HttpPut("{id:guid}/imagen")]
    [Authorize(Roles = "Administrador")]
    [RequestSizeLimit(LimiteSolicitudBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteSolicitudBytes)]
    public Task<IActionResult> CambiarImagen(Guid id, IFormFile? archivo, CancellationToken ct) =>
        Ejecutar(async () =>
        {
            await using var contenido = archivo?.OpenReadStream();
            var imagen = archivo is null || contenido is null ? null : new ImagenSubida(archivo.Length, contenido);
            return Ok(await _catalogo.CambiarImagenAsync(id, imagen, ct));
        });

    // RF-DOC-04, RD-06
    [HttpDelete("{id:guid}/imagen")]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> QuitarImagen(Guid id, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _catalogo.QuitarImagenAsync(id, ct)));

    // FN-01, RF-NEG-06
    [HttpGet("{id:guid}/imagen")]
    public Task<IActionResult> ObtenerImagen(Guid id, CancellationToken ct) =>
        Ejecutar(async () =>
        {
            var imagen = await _catalogo.ObtenerImagenAsync(id, ct);
            Response.Headers.XContentTypeOptions = "nosniff";
            Response.Headers.CacheControl = "private, max-age=86400";
            return File(imagen.Contenido, imagen.TipoContenido);
        });

    private static DatosComponente ADatos(GuardarComponenteRequest r) => new(
        r.Nombre,
        r.Categoria,
        r.Fabricante,
        r.SocketOInterfaz,
        r.ConsumoWatts,
        r.PotenciaWatts,
        r.Precio,
        r.StockDisponible,
        r.TipoMemoria,
        r.RanurasMemoria);
}
