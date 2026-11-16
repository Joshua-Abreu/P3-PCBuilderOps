using Core.Auth.Exceptions;

namespace PCBuilderOps.Domain;

// RF-NEG-01
public class DetalleEnsamble
{
    public const int CantidadMaxima = 16;

    public static readonly string MensajeCantidadInvalida =
        $"La cantidad de cada componente debe estar entre 1 y {CantidadMaxima}.";

    public static bool CantidadValida(int cantidad) => cantidad is > 0 and <= CantidadMaxima;

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
        if (!CantidadValida(cantidad))
            throw new ValidacionException(MensajeCantidadInvalida);

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
