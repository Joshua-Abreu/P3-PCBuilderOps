using Core.Auth.Exceptions;
using PCBuilderOps.Exceptions;

namespace PCBuilderOps.Domain;

// RF-NEG-01
public class OrdenEnsamble
{
    public const int CodigoSeguimientoMaximo = 20;

    private readonly List<DetalleEnsamble> _detalles = [];
    private readonly List<PruebaRendimiento> _pruebas = [];

    private OrdenEnsamble() { }

    public Guid Id { get; private set; }
    public string CodigoSeguimiento { get; private set; } = string.Empty;
    public Guid ClienteUsuarioId { get; private set; }
    public Guid? TecnicoAsignadoId { get; private set; }
    public EstadoOrdenEnsamble Estado { get; private set; }
    public decimal CostoTotal { get; private set; }
    public DateTime FechaCreacionUtc { get; private set; }

    public IReadOnlyList<DetalleEnsamble> Detalles => _detalles;
    public IReadOnlyList<PruebaRendimiento> Pruebas => _pruebas;

    // FN-01
    public static OrdenEnsamble Cotizar(
        Guid clienteUsuarioId,
        IReadOnlyCollection<(Componente Componente, int Cantidad)> componentes,
        DateTime ahoraUtc)
    {
        if (componentes.Count == 0)
            throw new ValidacionException("La cotización debe incluir al menos un componente.");

        var orden = new OrdenEnsamble
        {
            Id = Guid.NewGuid(),
            ClienteUsuarioId = clienteUsuarioId,
            Estado = EstadoOrdenEnsamble.Cotizada,
            FechaCreacionUtc = ahoraUtc
        };

        orden.CodigoSeguimiento = $"ORD-{orden.Id:N}"[..12].ToUpperInvariant();
        orden._detalles.AddRange(componentes.Select(c => DetalleEnsamble.Crear(orden.Id, c.Componente, c.Cantidad)));
        orden.CostoTotal = orden._detalles.Sum(d => d.Subtotal);

        return orden;
    }

    // FN-02, RF-NEG-04, RF-NEG-05
    public void IniciarArmado(Guid tecnicoId)
    {
        TransicionarA(EstadoOrdenEnsamble.EnArmado, () =>
        {
            foreach (var detalle in _detalles)
                detalle.Componente.ReservarStock(detalle.Cantidad);

            TecnicoAsignadoId = tecnicoId;
        });
    }

    // FN-04
    public void PasarAPruebas() => TransicionarA(EstadoOrdenEnsamble.EnPruebas);

    // FN-03
    public PruebaRendimiento RegistrarPrueba(
        string? tipoSoftwarePrueba,
        decimal temperaturaMaximaCpu,
        decimal temperaturaMaximaGpu,
        int puntajeBenchmark,
        bool esAprobada,
        DateTime ahoraUtc)
    {
        if (!MaquinaEstadosOrdenEnsamble.PermiteRegistrarPruebas(Estado))
            throw new PruebaFueraDeEstadoException(Estado);

        var prueba = PruebaRendimiento.Crear(
            Id, tipoSoftwarePrueba, temperaturaMaximaCpu, temperaturaMaximaGpu, puntajeBenchmark, esAprobada, ahoraUtc);

        _pruebas.Add(prueba);

        return prueba;
    }

    // FN-04
    public void Entregar() => TransicionarA(EstadoOrdenEnsamble.Entregada, ExigirPruebaMasRecienteAprobada);

    // RD-04: toda transición de la orden pasa por aquí.
    internal void TransicionarA(EstadoOrdenEnsamble hacia, Action? alCumplirTransicion = null)
    {
        MaquinaEstadosOrdenEnsamble.ValidarTransicion(Estado, hacia);

        alCumplirTransicion?.Invoke();

        Estado = hacia;
    }

    private void ExigirPruebaMasRecienteAprobada()
    {
        var pruebaMasReciente = _pruebas.MaxBy(p => p.FechaEjecucionUtc);

        if (pruebaMasReciente is null || !pruebaMasReciente.EsAprobada)
            throw new PruebasNoAprobadasException();
    }
}
