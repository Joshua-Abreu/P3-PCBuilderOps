using Core.Permisos.Domain;
using Core.Permisos.Exceptions;

namespace Core.Tests.Permisos;

// RD-04, RF-GP-06
public class MaquinaEstadosSolicitudTests
{
    private static readonly (EstadoSolicitud Desde, EstadoSolicitud Hacia)[] TransicionesDeLaTabla =
    [
        (EstadoSolicitud.Pendiente, EstadoSolicitud.Aprobada),
        (EstadoSolicitud.Aprobada, EstadoSolicitud.Aplicada),
        (EstadoSolicitud.Pendiente, EstadoSolicitud.Rechazada)
    ];

    public static TheoryData<EstadoSolicitud, EstadoSolicitud> TransicionesValidas()
    {
        var datos = new TheoryData<EstadoSolicitud, EstadoSolicitud>();
        foreach (var (desde, hacia) in TransicionesDeLaTabla)
            datos.Add(desde, hacia);
        return datos;
    }

    public static TheoryData<EstadoSolicitud, EstadoSolicitud> TransicionesFueraDeLaTabla()
    {
        var datos = new TheoryData<EstadoSolicitud, EstadoSolicitud>();
        foreach (var desde in Enum.GetValues<EstadoSolicitud>())
        foreach (var hacia in Enum.GetValues<EstadoSolicitud>())
            if (!TransicionesDeLaTabla.Contains((desde, hacia)))
                datos.Add(desde, hacia);
        return datos;
    }

    [Theory]
    [MemberData(nameof(TransicionesValidas))]
    public void EsTransicionValida_TransicionDeLaTabla_DevuelveVerdadero(EstadoSolicitud desde, EstadoSolicitud hacia)
    {
        // Preparar: la transición llega desde TransicionesValidas

        // Actuar
        var esValida = MaquinaEstadosSolicitud.EsTransicionValida(desde, hacia);

        // Verificar
        Assert.True(esValida);
    }

    [Theory]
    [MemberData(nameof(TransicionesFueraDeLaTabla))]
    public void EsTransicionValida_TransicionFueraDeLaTabla_DevuelveFalso(EstadoSolicitud desde, EstadoSolicitud hacia)
    {
        // Preparar: la transición llega desde TransicionesFueraDeLaTabla

        // Actuar
        var esValida = MaquinaEstadosSolicitud.EsTransicionValida(desde, hacia);

        // Verificar
        Assert.False(esValida);
    }

    [Theory]
    [MemberData(nameof(TransicionesFueraDeLaTabla))]
    public void ValidarTransicion_TransicionFueraDeLaTabla_LanzaTransicionNoPermitida(EstadoSolicitud desde, EstadoSolicitud hacia)
    {
        // Preparar
        var validar = () => MaquinaEstadosSolicitud.ValidarTransicion(desde, hacia);

        // Actuar
        var excepcion = Record.Exception(validar);

        // Verificar
        var rechazo = Assert.IsType<TransicionNoPermitidaException>(excepcion);
        Assert.Equal(desde, rechazo.Desde);
        Assert.Equal(hacia, rechazo.Hacia);
    }
}
