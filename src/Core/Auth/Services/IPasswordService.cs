namespace Core.Auth.Services;

public interface IPasswordService
{
    Task OlvidePasswordAsync(string correo, CancellationToken ct = default);
    Task RestablecerPasswordAsync(string token, string nuevaPassword, CancellationToken ct = default);
    Task ForzarCambioPasswordAsync(Guid usuarioId, CancellationToken ct = default);
    Task CambiarPasswordAsync(Guid usuarioId, string passwordActual, string nuevaPassword, CancellationToken ct = default);
}
