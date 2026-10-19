using Core.Permisos.Domain;
using Microsoft.AspNetCore.Authorization;

namespace API.Autorizacion;

public static class PoliticasPermiso
{
    public const string OperarTaller = "Permiso." + nameof(TipoPermiso.OperarTaller);

    public static void Registrar(AuthorizationOptions opciones) =>
        opciones.AddPolicy(OperarTaller, politica => politica
            .RequireAuthenticatedUser()
            .AddRequirements(new PermisoRequirement(TipoPermiso.OperarTaller)));
}
