using Core.Auth.Domain;

namespace Core.Auth.Services;

public interface IUsuarioAdminService
{
    Task<PaginacionUsuarios> ListarAsync(int pagina, int tamanoPagina, RolUsuario? rol, bool? activo, CancellationToken ct = default);
    Task<UsuarioDetalle?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task<UsuarioDetalle> CrearAsync(string correo, string password, RolUsuario rol, bool activarInmediatamente, CancellationToken ct = default);
    Task CambiarRolAsync(Guid id, RolUsuario nuevoRol, Guid adminId, CancellationToken ct = default);
    Task CambiarEstadoAsync(Guid id, bool activar, Guid adminId, CancellationToken ct = default);
}

public record PaginacionUsuarios(int Total, int Pagina, int TamanoPagina, IReadOnlyList<UsuarioDetalle> Usuarios);
public record UsuarioDetalle(Guid Id, string Correo, string Rol, bool EstaActivo, DateTime FechaCreacionUtc, bool DebeCambiarPassword);
