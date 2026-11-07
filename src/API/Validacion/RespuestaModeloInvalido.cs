using API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Validacion;

// RD-07, RD-08
public static class RespuestaModeloInvalido
{
    private const string MensajeBase = "Datos de entrada inválidos.";
    private const string PrefijoRutaJson = "$.";
    private const string MensajeJsonInvalido = "El cuerpo de la petición no es un JSON válido.";
    private const string RaizJson = "$";
    private const string MensajeFormulario = "No se pudo leer el formulario. Revisa que el archivo no supere el tamaño máximo de 10 MB.";

    public static IActionResult Crear(ActionContext context)
    {
        var parametrosDeCuerpo = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name);

        var mensaje = ConstruirMensaje(context.ModelState, parametrosDeCuerpo);

        // RF-DOC-01: un formulario que no se puede leer (por ejemplo, demasiado grande) no indica ningún campo.
        if (mensaje == MensajeBase && context.HttpContext.Request.HasFormContentType)
            mensaje = MensajeFormulario;

        return new BadRequestObjectResult(new MensajeRespuesta(mensaje));
    }

    public static string ConstruirMensaje(ModelStateDictionary modelState, IEnumerable<string> parametrosDeCuerpo)
    {
        var cuerpos = new HashSet<string>(parametrosDeCuerpo, StringComparer.OrdinalIgnoreCase);

        if (modelState.Any(entrada => entrada.Key == RaizJson && entrada.Value is { Errors.Count: > 0 }))
            return MensajeJsonInvalido;

        var campos = modelState
            .Where(entrada => entrada.Value is { Errors.Count: > 0 })
            .Select(entrada => NormalizarCampo(entrada.Key))
            .Where(campo => campo.Length > 0 && !cuerpos.Contains(campo))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(campo => campo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return campos.Count == 0
            ? MensajeBase
            : $"{MensajeBase} Revisa: {string.Join(", ", campos)}.";
    }

    private static string NormalizarCampo(string clave)
    {
        var campo = clave.StartsWith(PrefijoRutaJson, StringComparison.Ordinal) ? clave[PrefijoRutaJson.Length..] : clave;
        return campo.Length == 0 ? campo : char.ToLowerInvariant(campo[0]) + campo[1..];
    }
}
