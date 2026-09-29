namespace API.DTOs;

/// <summary>
/// DTO de entrada para registro de usuario.
/// RF-CA-01: Correo único. RF-CA-14: Política de contraseña.
/// </summary>
public record RegistroRequest(string Correo, string Password);

/// <summary>
/// DTO de entrada para reenvío de activación.
/// RF-CA-17: No filtrar correos registrados.
/// </summary>
public record ReenviarActivacionRequest(string Correo);

/// <summary>
/// DTO de salida con mensaje genérico.
/// </summary>
public record MensajeRespuesta(string Mensaje);
