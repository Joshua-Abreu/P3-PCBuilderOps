using PCBuilderOps.Exceptions;

namespace PCBuilderOps.Domain;

// RD-04, RF-NEG-03, RF-NEG-04, RF-NEG-05, RF-NEG-09
public static class MaquinaEstadosOrdenEnsamble
{
    private static readonly Dictionary<EstadoOrdenEnsamble, HashSet<EstadoOrdenEnsamble>> TransicionesValidas = new()
    {
        [EstadoOrdenEnsamble.Cotizada] = [EstadoOrdenEnsamble.EnArmado],
        [EstadoOrdenEnsamble.EnArmado] = [EstadoOrdenEnsamble.EnPruebas],
        [EstadoOrdenEnsamble.EnPruebas] = [EstadoOrdenEnsamble.Entregada],
        [EstadoOrdenEnsamble.Entregada] = [EstadoOrdenEnsamble.Cotizada]
    };

    public static bool EsTransicionValida(EstadoOrdenEnsamble desde, EstadoOrdenEnsamble hacia) =>
        TransicionesValidas.TryGetValue(desde, out var destinos) && destinos.Contains(hacia);

    public static void ValidarTransicion(EstadoOrdenEnsamble desde, EstadoOrdenEnsamble hacia)
    {
        if (!EsTransicionValida(desde, hacia))
            throw new TransicionOrdenNoPermitidaException(desde, hacia);
    }

    public static bool PermiteRegistrarPruebas(EstadoOrdenEnsamble estado) =>
        estado == EstadoOrdenEnsamble.EnPruebas;
}
