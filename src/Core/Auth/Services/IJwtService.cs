using System.Security.Claims;
using Core.Auth.Domain;

namespace Core.Auth.Services;

public interface IJwtService
{
    string GenerarToken(Guid usuarioId, string correo, RolUsuario rol);
    ClaimsPrincipal? ValidarToken(string token);
}
