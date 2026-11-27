namespace Core.Auditoria.Exceptions;

public class AccesoAuditoriaDenegadoException : Exception
{
    public AccesoAuditoriaDenegadoException()
        : base("Solo un Administrador puede consultar el historial de auditoría.") { }
}

public class HistorialInmutableException : InvalidOperationException
{
    public HistorialInmutableException()
        : base("El historial de auditoría no se puede modificar ni borrar.") { }
}
