namespace Core.Ordenes.Domain;

public static class MaquinaEstadosOrden
{
    private static readonly Dictionary<EstadoOrdenServicio, HashSet<EstadoOrdenServicio>> TransicionesValidas = new()
    {
        [EstadoOrdenServicio.Recibida] = new HashSet<EstadoOrdenServicio> { EstadoOrdenServicio.EnDiagnostico },
        [EstadoOrdenServicio.EnDiagnostico] = new HashSet<EstadoOrdenServicio> { EstadoOrdenServicio.EnProceso },
        [EstadoOrdenServicio.EnProceso] = new HashSet<EstadoOrdenServicio> { EstadoOrdenServicio.Entregada },
        [EstadoOrdenServicio.Entregada] = new HashSet<EstadoOrdenServicio>()
    };

    public static bool EsTransicionValida(EstadoOrdenServicio estadoActual, EstadoOrdenServicio nuevoEstado)
    {
        return TransicionesValidas.TryGetValue(estadoActual, out var destinos) && destinos.Contains(nuevoEstado);
    }

    public static void ValidarTransicion(EstadoOrdenServicio estadoActual, EstadoOrdenServicio nuevoEstado)
    {
        if (estadoActual == EstadoOrdenServicio.Entregada)
            throw new InvalidOperationException("La orden ya fue entregada. No se permiten transiciones desde el estado terminal.");

        if (estadoActual == EstadoOrdenServicio.Recibida && nuevoEstado == EstadoOrdenServicio.Entregada)
            throw new InvalidOperationException("Transición prohibida: no se puede pasar directo de Recibida a Entregada.");

        if (!EsTransicionValida(estadoActual, nuevoEstado))
            throw new InvalidOperationException($"Transición no válida: {estadoActual} -> {nuevoEstado}.");
    }
}
