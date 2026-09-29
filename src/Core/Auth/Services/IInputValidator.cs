namespace Core.Auth.Services;

/// <summary>
/// Validador de políticas de entrada para datos de autenticación.
/// RD-07: Validación de entradas malformadas o vacías.
/// RF-CA-14: Política de contraseñas seguras.
/// </summary>
public interface IInputValidator
{
    /// <summary>Valida que el correo tenga formato válido.</summary>
    void ValidarCorreo(string correo);

    /// <summary>Valida que la contraseña cumpla la política de seguridad.</summary>
    void ValidarPassword(string password);
}
