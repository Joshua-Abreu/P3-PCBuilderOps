namespace Core.Auditoria.Domain;

public static class AccionAuditoria
{
    // RF-AUD-02
    public const string SolicitudAprobada = "SolicitudAprobada";
    public const string SolicitudRechazada = "SolicitudRechazada";

    // RF-AUD-03, RF-DOC-06
    public const string DocumentoSubido = "DocumentoSubido";
    public const string DocumentoEliminado = "DocumentoEliminado";

    // RF-AUD-04, RF-CA-08, RF-CA-20
    public const string RolCambiado = "RolCambiado";
    public const string UsuarioDesactivado = "UsuarioDesactivado";
    public const string UsuarioReactivado = "UsuarioReactivado";

    // RF-AUD-05, RF-CA-13
    public const string RestablecimientoForzado = "RestablecimientoForzado";
    public const string ContrasenaRestablecida = "ContrasenaRestablecida";
    public const string ContrasenaCambiada = "ContrasenaCambiada";
}

public static class EntidadAuditada
{
    public const string SolicitudPermiso = "SolicitudPermiso";
    public const string Documento = "Documento";
    public const string Usuario = "Usuario";

    public static readonly IReadOnlyList<string> DelCore = [SolicitudPermiso, Documento, Usuario];
}

public static class IniciadoPor
{
    public const string Usuario = "Usuario";
    public const string Administrador = "Administrador";
}
