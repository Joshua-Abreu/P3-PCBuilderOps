namespace API.DTOs;

public record RegistroRequest(string Correo, string Password);
public record ReenviarActivacionRequest(string Correo);
public record MensajeRespuesta(string Mensaje);
