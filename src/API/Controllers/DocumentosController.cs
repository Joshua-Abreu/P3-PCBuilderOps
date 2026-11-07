using System.Security.Claims;
using API.DTOs;
using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Documentos.Exceptions;
using Core.Documentos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/documentos")]
[Authorize(Roles = "Estandar,Administrador")]
public class DocumentosController : ControllerBase
{
    private const long LimiteSolicitudBytes = DocumentosService.TamanoMaximoBytes + 1024 * 1024;

    private readonly IDocumentosService _documentos;

    public DocumentosController(IDocumentosService documentos)
    {
        _documentos = documentos;
    }

    // RF-DOC-01, RD-07
    [HttpPost]
    [RequestSizeLimit(LimiteSolicitudBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteSolicitudBytes)]
    public Task<IActionResult> Subir(IFormFile? archivo, CancellationToken ct) =>
        Ejecutar(async () =>
        {
            await using var contenido = archivo?.OpenReadStream();

            var subido = archivo is null || contenido is null
                ? null
                : new ArchivoSubido(archivo.FileName, archivo.ContentType, archivo.Length, contenido);

            var documento = await _documentos.SubirAsync(UsuarioActualId(), subido, ct);
            return StatusCode(StatusCodes.Status201Created, documento);
        });

    // RF-DOC-02
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _documentos.ListarAsync(ActorActual(), ct));

    // RF-DOC-01, RF-DOC-03
    [HttpGet("{id:guid}")]
    public Task<IActionResult> Obtener(Guid id, CancellationToken ct) =>
        Ejecutar(async () => Ok(await _documentos.ObtenerAsync(ActorActual(), id, ct)));

    // RF-DOC-03, RD-06
    [HttpGet("{id:guid}/descarga")]
    public Task<IActionResult> Descargar(Guid id, CancellationToken ct) =>
        Ejecutar(async () =>
        {
            var descarga = await _documentos.DescargarAsync(ActorActual(), id, ct);
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(descarga.Contenido, descarga.TipoContenido, descarga.Nombre);
        });

    // RF-DOC-04, RD-06
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Eliminar(Guid id, CancellationToken ct) =>
        Ejecutar(async () =>
        {
            await _documentos.EliminarAsync(ActorActual(), id, ct);
            return Ok(new MensajeRespuesta("Documento eliminado."));
        });

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
        catch (AccesoDocumentoDenegadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MensajeRespuesta(ex.Message));
        }
        catch (DocumentoNoEncontradoException ex)
        {
            return NotFound(new MensajeRespuesta(ex.Message));
        }
    }

    private Guid UsuarioActualId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private ActorDocumento ActorActual() =>
        new(UsuarioActualId(), User.IsInRole(nameof(RolUsuario.Administrador)));
}
