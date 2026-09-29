namespace Core.Auth.Services;

/// <summary>
/// Contrato del servicio de autenticación y gestión de cuentas.
/// RD-01: Responsabilidad única. RD-02: Sin lógica en controladores.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registra un nuevo usuario con cuenta inactiva y envía token de activación.
    /// RF-CA-01: Correo único. RF-CA-15: Activación por token. RF-NOT-08: Cola de correos.
    /// </summary>
    Task RegistrarAsync(string correo, string password, CancellationToken ct = default);

    /// <summary>
    /// Activa la cuenta de un usuario mediante un token válido.
    /// RF-CA-16: Validación de token y activación.
    /// </summary>
    Task ActivarCuentaAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Reenvía el correo de activación. Respuesta idéntica exista o no el correo.
    /// RF-CA-17: No filtrar correos registrados.
    /// </summary>
    Task ReenviarActivacionAsync(string correo, CancellationToken ct = default);
}
