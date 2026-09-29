using BCrypt.Net;

namespace Core.Auth.Services;

/// <summary>
/// Implementación de hash de contraseñas usando BCrypt.Net-Next.
/// RD-05: Hash y sal criptográfica. Jamás texto plano.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    // Work factor 12: balance entre seguridad y rendimiento
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verificar(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
