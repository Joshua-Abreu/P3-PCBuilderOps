namespace PCBuilderOps.Exceptions;

public class PermisoTallerRequeridoException : Exception
{
    public PermisoTallerRequeridoException()
        : base("Necesitas el permiso OperarTaller aprobado para esta operación.")
    {
    }
}
