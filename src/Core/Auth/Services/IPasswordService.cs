namespace Core.Auth.Services;

public interface IPasswordService
{
    Task OlvidePasswordAsync(string correo, CancellationToken ct = default);
    Task ValidarTokenRecuperacionAsync(string token, CancellationToken ct = default);
    Task RestablecerPasswordAsync(string token, string nuevaPassword, CancellationToken ct = default);
    Task ForzarRestablecimientoAsync(Guid usuarioId, CancellationToken ct = default);
    Task CambiarPasswordAsync(Guid usuarioId, string passwordActual, string nuevaPassword, CancellationToken ct = default);
}
