namespace Core.Auditoria.Domain;

// RF-AUD-01, RF-AUD-06
public class RegistroAuditoria
{
    public const int AccionMaximo = 50;
    public const int EntidadMaximo = 100;
    public const int EntidadIdMaximo = 100;

    private RegistroAuditoria() { }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Accion { get; private set; } = string.Empty;
    public string Entidad { get; private set; } = string.Empty;
    public string EntidadId { get; private set; } = string.Empty;
    public DateTime FechaUtc { get; private set; }
    public string ValorAnterior { get; private set; } = string.Empty;
    public string ValorNuevo { get; private set; } = string.Empty;

    public static RegistroAuditoria Crear(
        Guid usuarioId,
        string accion,
        string entidad,
        string entidadId,
        string valorAnterior,
        string valorNuevo,
        DateTime ahoraUtc)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El registro de auditoría necesita el usuario que actuó.", nameof(usuarioId));

        ArgumentException.ThrowIfNullOrWhiteSpace(accion);
        ArgumentException.ThrowIfNullOrWhiteSpace(entidad);
        ArgumentException.ThrowIfNullOrWhiteSpace(entidadId);
        ArgumentException.ThrowIfNullOrWhiteSpace(valorAnterior);
        ArgumentException.ThrowIfNullOrWhiteSpace(valorNuevo);

        if (accion.Length > AccionMaximo || entidad.Length > EntidadMaximo || entidadId.Length > EntidadIdMaximo)
            throw new ArgumentException("La acción, la entidad o su identificador superan su largo máximo.");

        return new RegistroAuditoria
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            FechaUtc = ahoraUtc,
            ValorAnterior = valorAnterior,
            ValorNuevo = valorNuevo
        };
    }
}
