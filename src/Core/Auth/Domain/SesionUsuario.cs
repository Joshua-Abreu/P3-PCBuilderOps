namespace Core.Auth.Domain;

public class SesionUsuario
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string TokenJwt { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime ExpiracionUtc { get; set; }
    public bool EstaRevocada { get; set; } = false;
    public DateTime? FechaRevocacionUtc { get; set; }
}
