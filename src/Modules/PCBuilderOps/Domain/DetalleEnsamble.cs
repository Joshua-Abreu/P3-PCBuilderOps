using Core.Auth.Exceptions;

namespace PCBuilderOps.Domain;

// RF-NEG-01
public class DetalleEnsamble
{
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
        if (cantidad <= 0)
            throw new ValidacionException("La cantidad de cada componente debe ser mayor que cero.");

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
