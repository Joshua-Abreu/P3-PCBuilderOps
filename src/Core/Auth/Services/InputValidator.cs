using System.Text.RegularExpressions;
using Core.Auth.Exceptions;

namespace Core.Auth.Services;

public class InputValidator : IInputValidator
{
    private static readonly Regex CorreoRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public void ValidarCorreo(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
            throw new ValidacionException("El correo es obligatorio.");

        if (!CorreoRegex.IsMatch(correo))
            throw new ValidacionException("El formato del correo no es válido.");
    }

    public void ValidarPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ValidacionException("La contraseña es obligatoria.");

        if (password.Length < 8)
            throw new ValidacionException("La contraseña debe tener al menos 8 caracteres.");

        if (!password.Any(char.IsLetter))
            throw new ValidacionException("La contraseña debe contener al menos una letra.");

        if (!password.Any(char.IsDigit))
            throw new ValidacionException("La contraseña debe contener al menos un número.");
    }
}
