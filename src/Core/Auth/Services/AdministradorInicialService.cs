using Core.Auth.Domain;
using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Core.Auth.Services;

// RF-CA-04, RD-10
public class AdministradorInicialService : IAdministradorInicialService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInputValidator _inputValidator;

    public AdministradorInicialService(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IInputValidator inputValidator)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _inputValidator = inputValidator;
    }

    public async Task<ResultadoAdministradorInicial> AsegurarAsync(string? correo, string? password, CancellationToken ct = default)
    {
        var existeAdministrador = await _dbContext.Usuarios
            .AnyAsync(u => u.Rol == RolUsuario.Administrador, ct);

        if (existeAdministrador)
            return ResultadoAdministradorInicial.YaExisteAdministrador;

        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            return ResultadoAdministradorInicial.SinConfiguracion;

        _inputValidator.ValidarCorreo(correo);
        _inputValidator.ValidarPassword(password);

        correo = correo.Trim().ToLowerInvariant();

        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo, ct);

        var resultado = ResultadoAdministradorInicial.Promovido;

        if (usuario is null)
        {
            usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Correo = correo,
                FechaCreacionUtc = DateTime.UtcNow
            };

            _dbContext.Usuarios.Add(usuario);
            resultado = ResultadoAdministradorInicial.Creado;
        }

        usuario.PasswordHash = _passwordHasher.Hash(password);
        usuario.Rol = RolUsuario.Administrador;
        usuario.EstaActivo = true;
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHastaUtc = null;

        await _dbContext.SaveChangesAsync(ct);

        return resultado;
    }
}
