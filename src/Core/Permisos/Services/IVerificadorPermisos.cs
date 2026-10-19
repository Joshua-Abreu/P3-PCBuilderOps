using Core.Permisos.Domain;

namespace Core.Permisos.Services;

public interface IVerificadorPermisos
{
    Task<bool> TienePermisoAsync(Guid usuarioId, TipoPermiso permiso, CancellationToken ct = default);
}
