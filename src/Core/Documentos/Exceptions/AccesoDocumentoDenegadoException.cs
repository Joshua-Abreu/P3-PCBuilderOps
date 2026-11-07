namespace Core.Documentos.Exceptions;

public class AccesoDocumentoDenegadoException : Exception
{
    public AccesoDocumentoDenegadoException()
        : base("Solo quien subió el documento o un Administrador puede usarlo.") { }
}
