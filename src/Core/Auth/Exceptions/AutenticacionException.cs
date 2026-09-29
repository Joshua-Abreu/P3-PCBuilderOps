namespace Core.Auth.Exceptions;

/// <summary>
/// Excepción de autenticación o autorización.
/// RD-08: Manejo de errores limpio, sin exponer detalles internos.
/// </summary>
public class AutenticacionException : Exception
{
    public AutenticacionException(string mensaje) : base(mensaje) { }
}
