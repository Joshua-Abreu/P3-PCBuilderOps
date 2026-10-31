using PCBuilderOps.Domain;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Domain;

// RD-04: una prueba por cada fila de las tablas de docs/maquina-de-estados.md.
public class MaquinaEstadosOrdenEnsambleTests
{
    // RF-NEG-03
    [Theory]
    [InlineData(EstadoOrdenEnsamble.Cotizada, EstadoOrdenEnsamble.EnArmado)]
    [InlineData(EstadoOrdenEnsamble.EnArmado, EstadoOrdenEnsamble.EnPruebas)]
    [InlineData(EstadoOrdenEnsamble.EnPruebas, EstadoOrdenEnsamble.Entregada)]
    public void TransicionarA_FilaPermitida_LaOrdenCambiaDeEstado(EstadoOrdenEnsamble desde, EstadoOrdenEnsamble hacia)
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(desde);

        // Actuar
        orden.TransicionarA(hacia);

        // Verificar
        Assert.Equal(hacia, orden.Estado);
    }

    // RF-NEG-04
    [Theory]
    [InlineData(EstadoOrdenEnsamble.Cotizada, EstadoOrdenEnsamble.Entregada)]
    [InlineData(EstadoOrdenEnsamble.Cotizada, EstadoOrdenEnsamble.EnPruebas)]
    [InlineData(EstadoOrdenEnsamble.Cotizada, EstadoOrdenEnsamble.Cotizada)]
    [InlineData(EstadoOrdenEnsamble.EnArmado, EstadoOrdenEnsamble.Cotizada)]
    [InlineData(EstadoOrdenEnsamble.EnArmado, EstadoOrdenEnsamble.EnArmado)]
    [InlineData(EstadoOrdenEnsamble.EnArmado, EstadoOrdenEnsamble.Entregada)]
    [InlineData(EstadoOrdenEnsamble.EnPruebas, EstadoOrdenEnsamble.Cotizada)]
    [InlineData(EstadoOrdenEnsamble.EnPruebas, EstadoOrdenEnsamble.EnArmado)]
    [InlineData(EstadoOrdenEnsamble.EnPruebas, EstadoOrdenEnsamble.EnPruebas)]
    [InlineData(EstadoOrdenEnsamble.Entregada, EstadoOrdenEnsamble.Cotizada)]
    [InlineData(EstadoOrdenEnsamble.Entregada, EstadoOrdenEnsamble.EnArmado)]
    [InlineData(EstadoOrdenEnsamble.Entregada, EstadoOrdenEnsamble.EnPruebas)]
    [InlineData(EstadoOrdenEnsamble.Entregada, EstadoOrdenEnsamble.Entregada)]
    public void TransicionarA_FilaProhibida_SeRechazaYElEstadoNoCambia(EstadoOrdenEnsamble desde, EstadoOrdenEnsamble hacia)
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(desde);

        // Actuar
        var accion = () => orden.TransicionarA(hacia);

        // Verificar
        Assert.Throws<TransicionOrdenNoPermitidaException>(accion);
        Assert.Equal(desde, orden.Estado);
    }

    // RF-NEG-05
    [Fact]
    public void EstadoTerminal_DesdeEntregadaNoSaleNingunaTransicion()
    {
        // Preparar
        var estados = Enum.GetValues<EstadoOrdenEnsamble>();

        // Actuar
        var destinos = estados.Where(hacia => MaquinaEstadosOrdenEnsamble.EsTransicionValida(EstadoOrdenEnsamble.Entregada, hacia));

        // Verificar
        Assert.Empty(destinos);
    }

    // RF-NEG-03
    [Fact]
    public void Estados_LaMaquinaDeclaraCuatroEstados()
    {
        // Preparar
        var estados = Enum.GetValues<EstadoOrdenEnsamble>();

        // Actuar
        var cantidad = estados.Length;

        // Verificar
        Assert.Equal(4, cantidad);
    }

    // FN-03, RD-04
    [Theory]
    [InlineData(EstadoOrdenEnsamble.Cotizada, false)]
    [InlineData(EstadoOrdenEnsamble.EnArmado, false)]
    [InlineData(EstadoOrdenEnsamble.EnPruebas, true)]
    [InlineData(EstadoOrdenEnsamble.Entregada, false)]
    public void PermiteRegistrarPruebas_SoloEnPruebasLoPermite(EstadoOrdenEnsamble estado, bool esperado)
    {
        // Preparar: el estado llega por parámetro

        // Actuar
        var permite = MaquinaEstadosOrdenEnsamble.PermiteRegistrarPruebas(estado);

        // Verificar
        Assert.Equal(esperado, permite);
    }
}
