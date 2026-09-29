namespace Core.Auth.Domain;

/// <summary>
/// Token de activación de cuenta de usuario.
/// RF-CA-15: Activación de cuenta mediante token con expiración.
/// RD-11: Todas las fechas en UTC.
/// </summary>
public class TokenActivacion
{
    public Guid Id { get; set; }

    /// <summary>ID del usuario al que pertenece el token.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Valor del token de activación.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Fecha de expiración del token en UTC (RD-11).</summary>
    public DateTime ExpiracionUtc { get; set; }

    /// <summary>Indica si el token ya fue utilizado.</summary>
    public bool FueUsado { get; set; }
}
