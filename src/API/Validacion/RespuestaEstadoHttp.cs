using Microsoft.AspNetCore.Diagnostics;

namespace API.Validacion;

// RD-07, RD-08
public static class RespuestaEstadoHttp
{
    public static Task Escribir(StatusCodeContext contexto)
    {
        var respuesta = contexto.HttpContext.Response;

        var mensaje = respuesta.StatusCode switch
        {
            StatusCodes.Status404NotFound => "El recurso solicitado no existe.",
            StatusCodes.Status405MethodNotAllowed => "Este método no está permitido en esta ruta.",
            StatusCodes.Status415UnsupportedMediaType => "El cuerpo de la petición debe enviarse como JSON.",
            _ => "La petición no se pudo procesar."
        };

        return respuesta.WriteAsJsonAsync(new { mensaje });
    }
}
