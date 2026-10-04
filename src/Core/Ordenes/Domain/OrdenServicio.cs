namespace Core.Ordenes.Domain;

public class OrdenServicio
{
    public Guid Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public EstadoOrdenServicio Estado { get; set; }
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime? FechaActualizacionUtc { get; set; }
}
