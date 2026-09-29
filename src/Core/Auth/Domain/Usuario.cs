namespace Core.Auth.Domain;

/// <summary>
/// Entidad de usuario del sistema de control de acceso.
/// RF-CA-01: Correo único como identificador de acceso.
/// RF-CA-15: Activación de cuenta mediante token.
/// RD-05: Contraseña almacenada como hash (BCrypt), nunca texto plano.
/// RD-11: Todas las fechas en UTC.
/// </summary>
public class Usuario
{
    public Guid Id { get; set; }

    /// <summary>Correo electrónico único del usuario (RF-CA-01).</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Hash BCrypt de la contraseña (RD-05).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Rol asignado al usuario (RF-CA-04).</summary>
    public RolUsuario Rol { get; set; }

    /// <summary>Indica si la cuenta ha sido activada (RF-CA-15). Por defecto false.</summary>
    public bool EstaActivo { get; set; } = false;

    /// <summary>Fecha de creación de la cuenta en UTC (RD-11).</summary>
    public DateTime FechaCreacionUtc { get; set; }

    /// <summary>Contador de intentos de inicio de sesión fallidos.</summary>
    public int IntentosFallidos { get; set; }

    /// <summary>Fecha hasta la cual la cuenta permanece bloqueada (UTC). Null si no está bloqueada.</summary>
    public DateTime? BloqueadoHastaUtc { get; set; }
}
