using Core.Auth.Exceptions;
using PCBuilderOps.Domain;
using PCBuilderOps.Exceptions;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Domain;

public class OrdenEnsambleTests
{
    private static readonly DateTime Antes = OrdenesDePrueba.Fecha;
    private static readonly DateTime Despues = OrdenesDePrueba.Fecha.AddHours(1);

    // FN-01
    [Fact]
    public void Cotizar_ConComponentes_NaceCotizadaConCostoTotalYSinTecnico()
    {
        // Preparar
        (Componente, int)[] componentes = [(OrdenesDePrueba.Cpu(), 1), (OrdenesDePrueba.Gpu(), 2)];

        // Actuar
        var orden = OrdenEnsamble.Cotizar(OrdenesDePrueba.ClienteId, componentes, OrdenesDePrueba.Fecha);

        // Verificar
        Assert.Equal(EstadoOrdenEnsamble.Cotizada, orden.Estado);
        Assert.Equal(300m + 2 * 400m, orden.CostoTotal);
        Assert.Null(orden.TecnicoAsignadoId);
    }

    // FN-01
    [Fact]
    public void Cotizar_SinComponentes_SeRechaza()
    {
        // Preparar
        (Componente, int)[] componentes = [];

        // Actuar
        var accion = () => OrdenEnsamble.Cotizar(OrdenesDePrueba.ClienteId, componentes, OrdenesDePrueba.Fecha);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
    }

    // FN-01, RD-07
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(DetalleEnsamble.CantidadMaxima + 1)]
    public void Cotizar_CantidadFueraDeRango_SeRechaza(int cantidad)
    {
        // Preparar
        (Componente, int)[] componentes = [(OrdenesDePrueba.Gpu(), cantidad)];

        // Actuar
        var accion = () => OrdenEnsamble.Cotizar(OrdenesDePrueba.ClienteId, componentes, OrdenesDePrueba.Fecha);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
    }

    // FN-02
    [Fact]
    public void IniciarArmado_OrdenCotizada_AsignaElTecnicoYReservaStock()
    {
        // Preparar
        var cpu = OrdenesDePrueba.Cpu(stock: 5);
        var orden = OrdenEnsamble.Cotizar(OrdenesDePrueba.ClienteId, [(cpu, 1)], OrdenesDePrueba.Fecha);

        // Actuar
        orden.IniciarArmado(OrdenesDePrueba.TecnicoId);

        // Verificar
        Assert.Equal(EstadoOrdenEnsamble.EnArmado, orden.Estado);
        Assert.Equal(OrdenesDePrueba.TecnicoId, orden.TecnicoAsignadoId);
        Assert.Equal(4, cpu.StockDisponible);
    }

    // FN-02
    [Fact]
    public void IniciarArmado_ConElUltimoStock_SePermite()
    {
        // Preparar
        var orden = OrdenesDePrueba.Cotizada(stockCpu: 1);

        // Actuar
        orden.IniciarArmado(OrdenesDePrueba.TecnicoId);

        // Verificar
        Assert.Equal(EstadoOrdenEnsamble.EnArmado, orden.Estado);
    }

    // FN-02
    [Fact]
    public void IniciarArmado_SinStock_SeRechazaYSigueCotizadaSinTecnico()
    {
        // Preparar
        var orden = OrdenesDePrueba.Cotizada(stockCpu: 0);

        // Actuar
        var accion = () => orden.IniciarArmado(OrdenesDePrueba.TecnicoId);

        // Verificar
        Assert.Throws<StockInsuficienteException>(accion);
        Assert.Equal(EstadoOrdenEnsamble.Cotizada, orden.Estado);
        Assert.Null(orden.TecnicoAsignadoId);
    }

    // FN-02, RF-NEG-04
    [Fact]
    public void IniciarArmado_OrdenYaEnArmado_SeRechazaYNoReservaOtraVez()
    {
        // Preparar
        var cpu = OrdenesDePrueba.Cpu(stock: 5);
        var orden = OrdenEnsamble.Cotizar(OrdenesDePrueba.ClienteId, [(cpu, 1)], OrdenesDePrueba.Fecha);
        orden.IniciarArmado(OrdenesDePrueba.TecnicoId);

        // Actuar
        var accion = () => orden.IniciarArmado(OrdenesDePrueba.TecnicoId);

        // Verificar
        Assert.Throws<TransicionOrdenNoPermitidaException>(accion);
        Assert.Equal(4, cpu.StockDisponible);
    }

    // RF-NEG-04
    [Fact]
    public void Entregar_OrdenCotizada_SeRechazaComoTransicionProhibidaYSigueCotizada()
    {
        // Preparar
        var orden = OrdenesDePrueba.Cotizada();

        // Actuar
        var accion = () => orden.Entregar();

        // Verificar
        Assert.Throws<TransicionOrdenNoPermitidaException>(accion);
        Assert.Equal(EstadoOrdenEnsamble.Cotizada, orden.Estado);
    }

    // RF-NEG-05
    [Fact]
    public void Entregar_OrdenYaEntregada_SeRechazaYSigueEntregada()
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(EstadoOrdenEnsamble.Entregada);

        // Actuar
        var accion = () => orden.Entregar();

        // Verificar
        Assert.Throws<TransicionOrdenNoPermitidaException>(accion);
        Assert.Equal(EstadoOrdenEnsamble.Entregada, orden.Estado);
    }

    // FN-04
    [Fact]
    public void Entregar_SinPruebas_SeRechazaYSigueEnPruebas()
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(EstadoOrdenEnsamble.EnPruebas);

        // Actuar
        var accion = () => orden.Entregar();

        // Verificar
        Assert.Throws<PruebasNoAprobadasException>(accion);
        Assert.Equal(EstadoOrdenEnsamble.EnPruebas, orden.Estado);
    }

    // FN-04
    [Fact]
    public void Entregar_LaMasRecienteEstaReprobada_SeRechazaAunqueHayaUnaAprobadaAnterior()
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(EstadoOrdenEnsamble.EnPruebas);
        OrdenesDePrueba.RegistrarPrueba(orden, esAprobada: true, Antes);
        OrdenesDePrueba.RegistrarPrueba(orden, esAprobada: false, Despues);

        // Actuar
        var accion = () => orden.Entregar();

        // Verificar
        Assert.Throws<PruebasNoAprobadasException>(accion);
        Assert.Equal(EstadoOrdenEnsamble.EnPruebas, orden.Estado);
    }

    // FN-04
    [Fact]
    public void Entregar_LaMasRecienteEstaAprobada_SeEntregaAunqueHayaUnaReprobadaAnterior()
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(EstadoOrdenEnsamble.EnPruebas);
        OrdenesDePrueba.RegistrarPrueba(orden, esAprobada: false, Antes);
        OrdenesDePrueba.RegistrarPrueba(orden, esAprobada: true, Despues);

        // Actuar
        orden.Entregar();

        // Verificar
        Assert.Equal(EstadoOrdenEnsamble.Entregada, orden.Estado);
    }

    // FN-04
    [Fact]
    public void Entregar_LaPruebaRegistradaAntesTieneFechaPosterior_DecideLaFechaNoElOrdenDeRegistro()
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(EstadoOrdenEnsamble.EnPruebas);
        OrdenesDePrueba.RegistrarPrueba(orden, esAprobada: true, Despues);
        OrdenesDePrueba.RegistrarPrueba(orden, esAprobada: false, Antes);

        // Actuar
        orden.Entregar();

        // Verificar
        Assert.Equal(EstadoOrdenEnsamble.Entregada, orden.Estado);
    }

    // FN-03
    [Fact]
    public void RegistrarPrueba_OrdenEnPruebas_GuardaLaPruebaYNoCambiaElEstado()
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(EstadoOrdenEnsamble.EnPruebas);

        // Actuar
        var prueba = orden.RegistrarPrueba("  Cinebench  ", 70.5m, 65m, 12000, false, Antes);

        // Verificar
        Assert.Equal(EstadoOrdenEnsamble.EnPruebas, orden.Estado);
        Assert.Same(prueba, Assert.Single(orden.Pruebas));
        Assert.Equal("Cinebench", prueba.TipoSoftwarePrueba);
        Assert.False(prueba.EsAprobada);
        Assert.Equal(Antes, prueba.FechaEjecucionUtc);
    }

    // FN-03
    [Theory]
    [InlineData(EstadoOrdenEnsamble.Cotizada)]
    [InlineData(EstadoOrdenEnsamble.EnArmado)]
    [InlineData(EstadoOrdenEnsamble.Entregada)]
    public void RegistrarPrueba_FueraDeEnPruebas_SeRechazaYNoSeGuarda(EstadoOrdenEnsamble estado)
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(estado);
        var pruebasAntes = orden.Pruebas.Count;

        // Actuar
        var accion = () => OrdenesDePrueba.RegistrarPrueba(orden, esAprobada: true, Despues);

        // Verificar
        Assert.Throws<PruebaFueraDeEstadoException>(accion);
        Assert.Equal(pruebasAntes, orden.Pruebas.Count);
    }

    // FN-03, RD-07
    [Theory]
    [InlineData(null, 70, 65, 100)]
    [InlineData("   ", 70, 65, 100)]
    [InlineData("Cinebench", 0, 65, 100)]
    [InlineData("Cinebench", 70, 0, 100)]
    [InlineData("Cinebench", 150.01, 65, 100)]
    [InlineData("Cinebench", 70, 150.01, 100)]
    [InlineData("Cinebench", 70, 65, -1)]
    public void RegistrarPrueba_DatosInvalidos_SeRechazaYNoSeGuarda(string? software, double cpu, double gpu, int puntaje)
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(EstadoOrdenEnsamble.EnPruebas);

        // Actuar
        var accion = () => orden.RegistrarPrueba(software, (decimal)cpu, (decimal)gpu, puntaje, true, Antes);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
        Assert.Empty(orden.Pruebas);
    }

    // FN-03, RD-07
    [Fact]
    public void RegistrarPrueba_TemperaturaEnElLimite_SeAcepta()
    {
        // Preparar
        var orden = OrdenesDePrueba.EnEstado(EstadoOrdenEnsamble.EnPruebas);

        // Actuar
        orden.RegistrarPrueba("Cinebench", PruebaRendimiento.TemperaturaMaximaValida, PruebaRendimiento.TemperaturaMaximaValida, 0, false, Antes);

        // Verificar
        Assert.Single(orden.Pruebas);
    }
}
