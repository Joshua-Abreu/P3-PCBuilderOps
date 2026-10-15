using API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Validacion;

// RD-07, RD-08
public static class RespuestaModeloInvalido
{
    private const string MensajeBase = "Datos de entrada inválidos.";
    private const string PrefijoRutaJson = "$.";

    public static IActionResult Crear(ActionContext context)
    {
        var parametrosDeCuerpo = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name);

        return new BadRequestObjectResult(new MensajeRespuesta(ConstruirMensaje(context.ModelState, parametrosDeCuerpo)));
    }

    public static string ConstruirMensaje(ModelStateDictionary modelState, IEnumerable<string> parametrosDeCuerpo)
    {
        var cuerpos = new HashSet<string>(parametrosDeCuerpo, StringComparer.OrdinalIgnoreCase);

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
