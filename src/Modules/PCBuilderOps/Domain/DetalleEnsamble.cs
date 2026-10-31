using Core.Auth.Exceptions;

namespace PCBuilderOps.Domain;

// RF-NEG-01
public class DetalleEnsamble
{
    public const int CantidadMaxima = 16;

    private DetalleEnsamble() { }

    public Guid Id { get; private set; }
    public Guid OrdenEnsambleId { get; private set; }
    public Guid ComponenteId { get; private set; }
    public Componente Componente { get; private set; } = null!;
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;

    internal static DetalleEnsamble Crear(Guid ordenEnsambleId, Componente componente, int cantidad)
    {
        if (cantidad is <= 0 or > CantidadMaxima)
            throw new ValidacionException($"La cantidad de cada componente debe estar entre 1 y {CantidadMaxima}.");

        return new DetalleEnsamble
        {
            Id = Guid.NewGuid(),
            OrdenEnsambleId = ordenEnsambleId,
            ComponenteId = componente.Id,
            Componente = componente,
            Cantidad = cantidad,
            PrecioUnitario = componente.Precio
        };
    }
}
