using System.Reflection;
using API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace API.Tests;

// RF-AUD-06, RF-AUD-07, RD-06 · criterios CA-AUD-13 y CA-AUD-16
public sealed class AuditoriaControllerTests
{
    private static readonly Assembly Api = typeof(AuditoriaController).Assembly;

    private static IEnumerable<(Type Controlador, MethodInfo Accion)> AccionesDe(Type controlador) =>
        controlador.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => (controlador, m));

    private static string RutaDe(Type controlador, MethodInfo accion)
    {
        var baseRuta = controlador.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
        var plantilla = accion.GetCustomAttributes<HttpMethodAttribute>().Select(a => a.Template).FirstOrDefault(t => t is not null);
        return plantilla is null ? baseRuta : $"{baseRuta}/{plantilla}";
    }

    // CA-AUD-13 · RF-AUD-06
    [Fact]
    public void ElControladorDeAuditoria_SoloExponeConsultasGet()
    {
        // Preparar
        var acciones = AccionesDe(typeof(AuditoriaController)).ToList();

        // Actuar
        var verbos = acciones
            .SelectMany(a => a.Accion.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Distinct()
            .ToList();

        // Verificar
        Assert.NotEmpty(acciones);
        Assert.Equal(new[] { "GET" }, verbos);
    }

    // CA-AUD-13 · RF-AUD-06
    [Fact]
    public void NingunControlador_ExponeUnaOperacionQueEditeOBorreElHistorial()
    {
        // Preparar
        var controladores = Api.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

        // Actuar
        var operacionesDeEscritura = controladores
            .SelectMany(AccionesDe)
            .Where(a => RutaDe(a.Controlador, a.Accion).Contains("auditoria", StringComparison.OrdinalIgnoreCase))
            .SelectMany(a => a.Accion.GetCustomAttributes<HttpMethodAttribute>().SelectMany(h => h.HttpMethods))
            .Where(verbo => verbo != "GET")
            .ToList();

        // Verificar
        Assert.Empty(operacionesDeEscritura);
    }

    // CA-AUD-16 · RF-AUD-07, RD-06
    [Fact]
    public void LaConsulta_ExigeElRolAdministradorYNingunaAccionLoSalta()
    {
        // Actuar
        var autorizacion = typeof(AuditoriaController).GetCustomAttribute<AuthorizeAttribute>();
        var anonimas = AccionesDe(typeof(AuditoriaController))
            .Where(a => a.Accion.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .ToList();

        // Verificar
        Assert.NotNull(autorizacion);
        Assert.Equal("Administrador", autorizacion.Roles);
        Assert.Null(typeof(AuditoriaController).GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Empty(anonimas);
    }
}
