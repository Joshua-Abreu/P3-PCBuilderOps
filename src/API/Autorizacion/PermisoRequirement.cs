using Core.Permisos.Domain;
using Microsoft.AspNetCore.Authorization;

namespace API.Autorizacion;

public record PermisoRequirement(TipoPermiso Permiso) : IAuthorizationRequirement;
