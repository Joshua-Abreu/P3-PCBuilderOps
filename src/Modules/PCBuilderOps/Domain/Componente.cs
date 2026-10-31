using PCBuilderOps.Exceptions;

namespace PCBuilderOps.Domain;

// RF-NEG-01
public class Componente
{
    public const int NombreMaximo = 150;
    public const int FabricanteMaximo = 100;
    public const int SocketOInterfazMaximo = 50;

    private Componente() { }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public CategoriaComponente Categoria { get; private set; }
    public string Fabricante { get; private set; } = string.Empty;
    public string SocketOInterfaz { get; private set; } = string.Empty;
    public int ConsumoWatts { get; private set; }
    public int? PotenciaWatts { get; private set; }
    public decimal Precio { get; private set; }
    public int StockDisponible { get; private set; }

    public static Componente Crear(
        Guid id,
        string nombre,
        CategoriaComponente categoria,
        string fabricante,
        string socketOInterfaz,
        int consumoWatts,
        int? potenciaWatts,
        decimal precio,
        int stockDisponible) => new()
    {
        Id = id,
        Nombre = nombre,
        Categoria = categoria,
        Fabricante = fabricante,
        SocketOInterfaz = socketOInterfaz,
        ConsumoWatts = consumoWatts,
        PotenciaWatts = potenciaWatts,
        Precio = precio,
        StockDisponible = stockDisponible
    };

    // FN-02
    public void ReservarStock(int cantidad)
    {
        if (cantidad > StockDisponible)
            throw new StockInsuficienteException(Nombre);

        StockDisponible -= cantidad;
    }
}
