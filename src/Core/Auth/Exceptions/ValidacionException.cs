namespace Core.Auth.Exceptions;

public class ValidacionException : Exception
{
    public ValidacionException(string mensaje) : base(mensaje) { }
}
