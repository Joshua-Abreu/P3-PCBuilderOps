namespace PCBuilderOps.Domain;

// FN-05, RF-NEG-08
public class TransicionOrden
{
    private TransicionOrden() { }

    public Guid Id { get; private set; }
    public Guid OrdenEnsambleId { get; private set; }
    public EstadoOrdenEnsamble? EstadoAnterior { get; private set; }
    public EstadoOrdenEnsamble EstadoNuevo { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTime FechaUtc { get; private set; }
    public long? SegundosEnEstadoAnterior { get; private set; }

    internal static TransicionOrden Crear(
        Guid ordenEnsambleId,
        EstadoOrdenEnsamble? estadoAnterior,
        EstadoOrdenEnsamble estadoNuevo,
        Guid usuarioId,
        DateTime fechaUtc,
        long? segundosEnEstadoAnterior) => new()
        {
            Id = Guid.NewGuid(),
            OrdenEnsambleId = ordenEnsambleId,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            UsuarioId = usuarioId,
            FechaUtc = fechaUtc,
            SegundosEnEstadoAnterior = segundosEnEstadoAnterior
        };
}
