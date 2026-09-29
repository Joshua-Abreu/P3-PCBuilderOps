namespace Core.Auth.Exceptions;

public class AutenticacionException : Exception
{
    public AutenticacionException(string mensaje) : base(mensaje) { }
}
