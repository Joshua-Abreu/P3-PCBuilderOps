namespace API.DTOs;

public record RegistroRequest(string Correo, string Password);
public record ReenviarActivacionRequest(string Correo);
public record MensajeRespuesta(string Mensaje);
public record LoginRequest(string Correo, string Password);
public record LoginRespuesta(string Token, DateTime ExpiracionUtc, bool DebeCambiarPassword);
public record UsuarioAutenticadoRespuesta(Guid Id, string Correo, string Rol);
public record OlvidePasswordRequest(string Correo);
public record RestablecerPasswordRequest(string Token, string NuevaPassword);
public record CrearUsuarioRequest(string Correo, string Password, string Rol, bool ActivarInmediatamente);
public record CambiarRolRequest(string Rol);
public record CambiarEstadoRequest(bool Activar);
public record CambiarPasswordRequest(string PasswordActual, string NuevaPassword);
