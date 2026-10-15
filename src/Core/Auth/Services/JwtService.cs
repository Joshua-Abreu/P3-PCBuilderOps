using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.Auth.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Core.Auth.Services;

public class JwtService : IJwtService
{
    private readonly ConfiguracionJwt _configuracion;

    public JwtService(ConfiguracionJwt configuracion)
    {
        _configuracion = configuracion;
    }

    public string GenerarToken(Guid usuarioId, string correo, RolUsuario rol, Guid sesionId)
    {
        var creds = new SigningCredentials(_configuracion.ClaveFirma, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, correo),
            new Claim(ClaimTypes.Role, rol.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, sesionId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuracion.Emisor,
            audience: _configuracion.Audiencia,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_configuracion.ExpiracionMinutos),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidarToken(string token)
    {
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, _configuracion.ParametrosValidacion(), out _);
        }
        catch
        {
            return null;
        }
    }
}
