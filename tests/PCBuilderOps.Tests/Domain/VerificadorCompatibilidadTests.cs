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
