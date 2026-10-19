using System.Security.Claims;
using Core.Auth.Domain;
using Core.Permisos.Services;
using Microsoft.AspNetCore.Authorization;

namespace API.Autorizacion;

// RF-GP-05, RD-06
public class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
{
    private readonly IVerificadorPermisos _verificadorPermisos;

    public PermisoAuthorizationHandler(IVerificadorPermisos verificadorPermisos)
    {
        _verificadorPermisos = verificadorPermisos;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermisoRequirement requirement)
    {
        if (context.User.IsInRole(nameof(RolUsuario.Administrador)))
        {
            context.Succeed(requirement);
            return;
        }

        var claimId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(claimId, out var usuarioId)
            && await _verificadorPermisos.TienePermisoAsync(usuarioId, requirement.Permiso))
        {
            context.Succeed(requirement);
        }
    }
}
