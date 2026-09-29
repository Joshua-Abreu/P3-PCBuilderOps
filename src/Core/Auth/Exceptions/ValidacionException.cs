namespace Core.Auth.Exceptions;

/// <summary>
/// Excepción de validación de datos de entrada.
/// RD-07: Datos malformados o vacíos devuelven 400 Bad Request controlado.
/// </summary>
public class ValidacionException : Exception
{
    public ValidacionException(string mensaje) : base(mensaje) { }
}
