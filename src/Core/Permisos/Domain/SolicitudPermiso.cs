using Core.Auth.Exceptions;
using Core.Permisos.Exceptions;

namespace Core.Permisos.Domain;

public class SolicitudPermiso
{
    public const int MotivoRechazoMaximo = 500;

    private SolicitudPermiso() { }

    public Guid Id { get; private set; }
    public Guid SolicitanteId { get; private set; }
    public TipoPermiso Permiso { get; private set; }
    public EstadoSolicitud Estado { get; private set; }
    public Guid? AprobadorId { get; private set; }
    public DateTime FechaCreacionUtc { get; private set; }
    public DateTime? FechaResolucionUtc { get; private set; }
    public string? MotivoRechazo { get; private set; }

    // RF-GP-01
    public static SolicitudPermiso Crear(Guid solicitanteId, TipoPermiso permiso, DateTime ahoraUtc) => new()
    {
        Id = Guid.NewGuid(),
        SolicitanteId = solicitanteId,
        Permiso = permiso,
        Estado = EstadoSolicitud.Aprobada,
        FechaCreacionUtc = ahoraUtc
    };

    // RF-GP-03
    public void Aprobar(Guid aprobadorId, DateTime ahoraUtc)
    {
        ValidarQueNoEsPropia(aprobadorId);
        MaquinaEstadosSolicitud.ValidarTransicion(Estado, EstadoSolicitud.Aprobada);

        Estado = EstadoSolicitud.Aprobada;
        AprobadorId = aprobadorId;
        FechaResolucionUtc = ahoraUtc;
    }

    // RF-GP-05
    public void Aplicar()
    {
        MaquinaEstadosSolicitud.ValidarTransicion(Estado, EstadoSolicitud.Aplicada);

        Estado = EstadoSolicitud.Aplicada;
    }

    // RF-GP-04
    public void Rechazar(Guid aprobadorId, string? motivo, DateTime ahoraUtc)
    {
        ValidarQueNoEsPropia(aprobadorId);

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ValidacionException("El motivo del rechazo es obligatorio.");

        var motivoLimpio = motivo.Trim();

        if (motivoLimpio.Length > MotivoRechazoMaximo)
            throw new ValidacionException($"El motivo del rechazo no puede superar {MotivoRechazoMaximo} caracteres.");

        MaquinaEstadosSolicitud.ValidarTransicion(Estado, EstadoSolicitud.Rechazada);

        Estado = EstadoSolicitud.Rechazada;
        AprobadorId = aprobadorId;
        FechaResolucionUtc = ahoraUtc;
        MotivoRechazo = motivoLimpio;
    }

    // RF-GP-07
    private void ValidarQueNoEsPropia(Guid aprobadorId)
    {
        if (aprobadorId == SolicitanteId)
            throw new ResolucionPropiaException();
    }
}
