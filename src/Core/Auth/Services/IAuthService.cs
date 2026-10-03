using Core.Auth.Domain;

namespace Core.Auth.Services;

public interface IAuthService
{
    Task RegistrarAsync(string correo, string password, CancellationToken ct = default);
    Task ActivarCuentaAsync(string token, CancellationToken ct = default);
    Task ReenviarActivacionAsync(string correo, CancellationToken ct = default);
    Task<LoginResultado> LoginAsync(string correo, string password, CancellationToken ct = default);
    Task LogoutAsync(string tokenJwt, CancellationToken ct = default);
    Task<UsuarioAutenticado?> ObtenerUsuarioAutenticadoAsync(CancellationToken ct = default);
}

public record LoginResultado(string Token, DateTime ExpiracionUtc, bool DebeCambiarPassword);
public record UsuarioAutenticado(Guid Id, string Correo, RolUsuario Rol);
