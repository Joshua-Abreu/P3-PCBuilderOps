namespace Core.Documentos.Exceptions;

public class DocumentoNoEncontradoException : Exception
{
    public DocumentoNoEncontradoException()
        : base("El documento no existe.") { }
}
