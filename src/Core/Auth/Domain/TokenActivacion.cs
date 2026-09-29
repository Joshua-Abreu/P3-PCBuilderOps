namespace Core.Auth.Domain;

public class TokenActivacion
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiracionUtc { get; set; }
    public bool FueUsado { get; set; }
}
