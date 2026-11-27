namespace Core.Auditoria.Services;

// RF-AUD-01, RD-03
public interface IAuditor
{
    void Registrar(Guid usuarioId, string accion, string entidad, string entidadId, object valorAnterior, object valorNuevo);
}
