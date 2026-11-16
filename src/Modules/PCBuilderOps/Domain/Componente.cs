using Core.Auth.Exceptions;
using PCBuilderOps.Exceptions;

namespace PCBuilderOps.Domain;

// RF-NEG-01
public class Componente
{
    public const int NombreMaximo = 150;
    public const int FabricanteMaximo = 100;
    public const int SocketOInterfazMaximo = 50;
    public const int TipoMemoriaMaximo = 20;

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
    public string? TipoMemoria { get; private set; }
    public int? RanurasMemoria { get; private set; }
    public bool EstaActivo { get; private set; } = true;
    public string? ImagenArchivo { get; private set; }
    public string? ImagenTipoContenido { get; private set; }

    // RF-NEG-01, RD-07
    public static Componente Registrar(Guid id, DatosComponente datos)
    {
        var validos = ValidadorComponente.Validar(datos);
        var componente = new Componente { Id = id, Categoria = validos.Categoria };
        componente.Aplicar(validos);
        return componente;
    }

    // RF-NEG-01, RD-07
    public void Actualizar(DatosComponente datos)
    {
        var validos = ValidadorComponente.Validar(datos);

        if (validos.Categoria != Categoria)
            throw new ValidacionException("La categoría de un componente no se puede cambiar; retíralo y crea uno nuevo.");

        Aplicar(validos);
    }

    public void CambiarEstado(bool estaActivo) => EstaActivo = estaActivo;

    public string? CambiarImagen(string archivo, string tipoContenido)
    {
        var anterior = ImagenArchivo;
        ImagenArchivo = archivo;
        ImagenTipoContenido = tipoContenido;
        return anterior;
    }

    public string? QuitarImagen()
    {
        var anterior = ImagenArchivo;
        ImagenArchivo = null;
        ImagenTipoContenido = null;
        return anterior;
    }

    private void Aplicar(DatosComponenteValidos datos)
    {
        Nombre = datos.Nombre;
        Fabricante = datos.Fabricante;
        SocketOInterfaz = datos.SocketOInterfaz;
        ConsumoWatts = datos.ConsumoWatts;
        PotenciaWatts = datos.PotenciaWatts;
        Precio = datos.Precio;
        StockDisponible = datos.StockDisponible;
        TipoMemoria = datos.TipoMemoria;
        RanurasMemoria = datos.RanurasMemoria;
    }

    public static Componente Crear(
        Guid id,
        string nombre,
        CategoriaComponente categoria,
        string fabricante,
        string socketOInterfaz,
        int consumoWatts,
        int? potenciaWatts,
        decimal precio,
        int stockDisponible,
        string? tipoMemoria = null,
        int? ranurasMemoria = null) => new()
    {
        Id = id,
        Nombre = nombre,
        Categoria = categoria,
        Fabricante = fabricante,
        SocketOInterfaz = socketOInterfaz,
        ConsumoWatts = consumoWatts,
        PotenciaWatts = potenciaWatts,
        Precio = precio,
        StockDisponible = stockDisponible,
        TipoMemoria = tipoMemoria,
        RanurasMemoria = ranurasMemoria
    };

    // FN-02
    public void ReservarStock(int cantidad)
    {
        if (cantidad > StockDisponible)
            throw new StockInsuficienteException(Nombre);

        StockDisponible -= cantidad;
    }
}
