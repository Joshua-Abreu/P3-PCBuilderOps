namespace Core.Auth.Services;

/// <summary>
/// Servicio de hash criptográfico de contraseñas.
/// RD-05: Las contraseñas se guardan siempre con hash y sal (BCrypt).
/// RF-CA-02: Verificación segura de credenciales.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Genera un hash BCrypt de la contraseña.</summary>
    string Hash(string password);

    /// <summary>Verifica si la contraseña coincide con el hash almacenado.</summary>
    bool Verificar(string password, string hash);
}
