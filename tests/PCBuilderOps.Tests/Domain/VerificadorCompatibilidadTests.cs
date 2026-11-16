using Core.Auth.Exceptions;
using PCBuilderOps.Domain;
using PCBuilderOps.Tests.Soporte;

namespace PCBuilderOps.Tests.Domain;

// FN-01
public class VerificadorCompatibilidadTests
{
    [Fact]
    public void Validar_ConsumoIgualALaPotenciaDeLaFuente_SeAcepta()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(consumoWatts: 100), 1),
            (OrdenesDePrueba.Placa(consumoWatts: 50), 1),
            (OrdenesDePrueba.Fuente(potenciaWatts: 150), 1)
        ];

        // Actuar
        var error = Record.Exception(() => VerificadorCompatibilidad.Validar(componentes));

        // Verificar
        Assert.Null(error);
    }

    [Fact]
    public void Validar_ConsumoUnWattSobreLaPotencia_SeRechaza()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(consumoWatts: 100), 1),
            (OrdenesDePrueba.Placa(consumoWatts: 51), 1),
            (OrdenesDePrueba.Fuente(potenciaWatts: 150), 1)
        ];

        // Actuar
        var accion = () => VerificadorCompatibilidad.Validar(componentes);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
    }

    [Fact]
    public void Validar_ElConsumoCuentaLaCantidadDeCadaComponente()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(consumoWatts: 100), 1),
            (OrdenesDePrueba.Placa(consumoWatts: 50), 1),
            (OrdenesDePrueba.Gpu(consumoWatts: 200), 2),
            (OrdenesDePrueba.Fuente(potenciaWatts: 500), 1)
        ];

        // Actuar
        var accion = () => VerificadorCompatibilidad.Validar(componentes);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
    }

    [Fact]
    public void Validar_SocketsDistintos_SeRechaza()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(socket: "LGA1700"), 1),
            (OrdenesDePrueba.Placa(socket: "AM5"), 1),
            (OrdenesDePrueba.Fuente(), 1)
        ];

        // Actuar
        var accion = () => VerificadorCompatibilidad.Validar(componentes);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
    }

    [Theory]
    [InlineData(CategoriaComponente.Cpu)]
    [InlineData(CategoriaComponente.PlacaBase)]
    [InlineData(CategoriaComponente.Fuente)]
    public void Validar_FaltaUnComponenteObligatorio_SeRechaza(CategoriaComponente categoriaFaltante)
    {
        // Preparar
        (Componente Componente, int Cantidad)[] todos =
        [
            (OrdenesDePrueba.Cpu(), 1),
            (OrdenesDePrueba.Placa(), 1),
            (OrdenesDePrueba.Fuente(), 1)
        ];
        var componentes = todos.Where(c => c.Componente.Categoria != categoriaFaltante).ToList();

        // Actuar
        var accion = () => VerificadorCompatibilidad.Validar(componentes);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
    }

    public static TheoryData<string, (Componente, int)[], string> PrimerErrorPorCaso => new()
    {
        { "sin CPU, sockets y potencia mal", [(OrdenesDePrueba.Placa(socket: "AM5"), 1), (OrdenesDePrueba.Fuente(potenciaWatts: 1), 1)], "La cotización debe incluir exactamente una CPU." },
        { "sin placa", [(OrdenesDePrueba.Cpu(), 1), (OrdenesDePrueba.Fuente(), 1)], "La cotización debe incluir exactamente una placa base." },
        { "sin fuente y sockets distintos", [(OrdenesDePrueba.Cpu(socket: "LGA1700"), 1), (OrdenesDePrueba.Placa(socket: "AM5"), 1)], "La cotización debe incluir exactamente una fuente de poder." },
        { "dos CPU", [(OrdenesDePrueba.Cpu(), 2), (OrdenesDePrueba.Placa(), 1), (OrdenesDePrueba.Fuente(), 1)], "La cotización debe incluir exactamente una CPU." },
        { "sockets distintos y sin potencia", [(OrdenesDePrueba.Cpu(socket: "LGA1700", consumoWatts: 500), 1), (OrdenesDePrueba.Placa(socket: "AM5"), 1), (OrdenesDePrueba.Fuente(potenciaWatts: 100), 1)], "La CPU usa LGA1700 y la placa base usa AM5." },
        { "potencia insuficiente", [(OrdenesDePrueba.Cpu(consumoWatts: 100), 1), (OrdenesDePrueba.Placa(consumoWatts: 51), 1), (OrdenesDePrueba.Fuente(potenciaWatts: 150), 1)], "La fuente entrega 150 W y los componentes consumen 151 W." }
    };

    [Theory]
    [MemberData(nameof(PrimerErrorPorCaso))]
    public void Validar_ConVariosProblemas_InformaElPrimeroConSuMensajeExacto(
        string caso,
        (Componente, int)[] componentes,
        string mensajeEsperado)
    {
        // Actuar
        var error = Assert.Throws<ValidacionException>(() => VerificadorCompatibilidad.Validar(componentes));

        // Verificar
        Assert.True(error.Message == mensajeEsperado, $"{caso}: {error.Message}");
    }

    [Fact]
    public void Evaluar_ArmadoVacio_ListaLosTresObligatoriosComoFaltantesSinProblemas()
    {
        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar([]);

        // Verificar
        Assert.False(resultado.EsCompatible);
        Assert.Equal([CategoriaComponente.Cpu, CategoriaComponente.PlacaBase, CategoriaComponente.Fuente], resultado.Faltantes);
        Assert.Empty(resultado.Problemas);
        Assert.Equal(0, resultado.ConsumoWatts);
        Assert.Null(resultado.PotenciaWatts);
    }

    [Fact]
    public void Evaluar_ArmadoCompatible_DevuelveConsumoYPotencia()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(consumoWatts: 100), 1),
            (OrdenesDePrueba.Placa(consumoWatts: 50), 1),
            (OrdenesDePrueba.Gpu(consumoWatts: 200), 1),
            (OrdenesDePrueba.Fuente(potenciaWatts: 500), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.True(resultado.EsCompatible);
        Assert.Equal(350, resultado.ConsumoWatts);
        Assert.Equal(500, resultado.PotenciaWatts);
    }

    [Fact]
    public void Evaluar_MemoriaDeOtroTipoQueLaPlaca_LaInformaComoProblema()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(), 1),
            (OrdenesDePrueba.Placa(tipoMemoria: "DDR5"), 1),
            (OrdenesDePrueba.Ram(tipo: "DDR4"), 1),
            (OrdenesDePrueba.Fuente(), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.False(resultado.EsCompatible);
        Assert.Equal(["La memoria RAM DDR4 es DDR4 y la placa base usa DDR5."], resultado.Problemas);
    }

    [Fact]
    public void Validar_MemoriaDeOtroTipoQueLaPlaca_SeRechaza()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(), 1),
            (OrdenesDePrueba.Placa(tipoMemoria: "DDR4"), 1),
            (OrdenesDePrueba.Ram(tipo: "DDR5"), 1),
            (OrdenesDePrueba.Fuente(), 1)
        ];

        // Actuar
        var accion = () => VerificadorCompatibilidad.Validar(componentes);

        // Verificar
        var error = Assert.Throws<ValidacionException>(accion);
        Assert.Equal("La memoria RAM DDR5 es DDR5 y la placa base usa DDR4.", error.Message);
    }

    [Fact]
    public void Evaluar_MemoriaDelMismoTipoQueLaPlaca_EsCompatible()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(), 1),
            (OrdenesDePrueba.Placa(tipoMemoria: "DDR5", ranurasMemoria: 4), 1),
            (OrdenesDePrueba.Ram(tipo: "DDR5"), 2),
            (OrdenesDePrueba.Fuente(), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.True(resultado.EsCompatible);
    }

    [Fact]
    public void Evaluar_MasModulosQueRanuras_LoInformaComoProblema()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(), 1),
            (OrdenesDePrueba.Placa(tipoMemoria: "DDR5", ranurasMemoria: 2), 1),
            (OrdenesDePrueba.Ram(tipo: "DDR5"), 3),
            (OrdenesDePrueba.Fuente(), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.Equal(
            ["La placa base tiene 2 ranuras de memoria y el armado lleva 3 módulos."],
            resultado.Problemas);
    }

    [Fact]
    public void Evaluar_ModulosIgualALasRanuras_EsCompatible()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(), 1),
            (OrdenesDePrueba.Placa(tipoMemoria: "DDR5", ranurasMemoria: 2), 1),
            (OrdenesDePrueba.Ram(tipo: "DDR5"), 1),
            (OrdenesDePrueba.Ram(tipo: "DDR5"), 1),
            (OrdenesDePrueba.Fuente(), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.True(resultado.EsCompatible);
    }

    [Fact]
    public void Evaluar_PlacaSinDatosDeMemoria_NoRevisaLaMemoria()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(), 1),
            (OrdenesDePrueba.Placa(), 1),
            (OrdenesDePrueba.Ram(tipo: "DDR4", consumoWatts: 1), 8),
            (OrdenesDePrueba.Fuente(), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.True(resultado.EsCompatible);
    }

    [Fact]
    public void Evaluar_ConsumoSobreElOchentaPorCientoDeLaFuente_AdvierteSinBloquear()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(consumoWatts: 351), 1),
            (OrdenesDePrueba.Placa(consumoWatts: 50), 1),
            (OrdenesDePrueba.Fuente(potenciaWatts: 500), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.True(resultado.EsCompatible);
        Assert.Equal(
            ["La fuente queda con menos del 20 % de margen: entrega 500 W y los componentes consumen 401 W."],
            resultado.Advertencias);
    }

    [Fact]
    public void Evaluar_ConsumoEnElOchentaPorCientoDeLaFuente_NoAdvierte()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(consumoWatts: 350), 1),
            (OrdenesDePrueba.Placa(consumoWatts: 50), 1),
            (OrdenesDePrueba.Fuente(potenciaWatts: 500), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public void Evaluar_ConsumoSobreLaPotencia_EsProblemaYNoAdvertencia()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(consumoWatts: 500), 1),
            (OrdenesDePrueba.Placa(consumoWatts: 50), 1),
            (OrdenesDePrueba.Fuente(potenciaWatts: 500), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.Single(resultado.Problemas);
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public void Evaluar_VariosProblemas_LosInformaTodosALaVez()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(socket: "LGA1700", consumoWatts: 300), 1),
            (OrdenesDePrueba.Placa(socket: "AM5"), 1),
            (OrdenesDePrueba.Fuente(potenciaWatts: 100), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.Empty(resultado.Faltantes);
        Assert.Equal(
            ["La CPU usa LGA1700 y la placa base usa AM5.", "La fuente entrega 100 W y los componentes consumen 350 W."],
            resultado.Problemas);
    }

    [Fact]
    public void Evaluar_SinFuente_IndicaElConsumoSinComparar()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(consumoWatts: 100), 1),
            (OrdenesDePrueba.Placa(consumoWatts: 50), 1)
        ];

        // Actuar
        var resultado = VerificadorCompatibilidad.Evaluar(componentes);

        // Verificar
        Assert.Equal([CategoriaComponente.Fuente], resultado.Faltantes);
        Assert.Empty(resultado.Problemas);
        Assert.Equal(150, resultado.ConsumoWatts);
        Assert.Null(resultado.PotenciaWatts);
    }

    [Fact]
    public void Validar_DosCpu_SeRechaza()
    {
        // Preparar
        (Componente, int)[] componentes =
        [
            (OrdenesDePrueba.Cpu(), 2),
            (OrdenesDePrueba.Placa(), 1),
            (OrdenesDePrueba.Fuente(), 1)
        ];

        // Actuar
        var accion = () => VerificadorCompatibilidad.Validar(componentes);

        // Verificar
        Assert.Throws<ValidacionException>(accion);
    }
}
