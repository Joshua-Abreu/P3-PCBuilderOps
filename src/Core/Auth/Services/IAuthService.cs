namespace Core.Auth.Services;

public interface IAuthService
{
    Task RegistrarAsync(string correo, string password, CancellationToken ct = default);
    Task ActivarCuentaAsync(string token, CancellationToken ct = default);
    Task ReenviarActivacionAsync(string correo, CancellationToken ct = default);
}
