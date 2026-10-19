namespace Core.Permisos.Domain;

// RF-GP-05
public class PermisoUsuario
{
    private PermisoUsuario() { }

    public Guid UsuarioId { get; private set; }
    public TipoPermiso Permiso { get; private set; }
    public Guid SolicitudId { get; private set; }
    public DateTime FechaOtorgadoUtc { get; private set; }

    public static PermisoUsuario OtorgarPor(SolicitudPermiso solicitud, DateTime ahoraUtc) => new()
    {
        UsuarioId = solicitud.SolicitanteId,
        Permiso = solicitud.Permiso,
        SolicitudId = solicitud.Id,
        FechaOtorgadoUtc = ahoraUtc
    };
}
