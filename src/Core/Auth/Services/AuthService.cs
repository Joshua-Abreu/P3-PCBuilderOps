using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Data;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Auth.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInputValidator _inputValidator;

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IInputValidator inputValidator)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _inputValidator = inputValidator;
    }

    public async Task RegistrarAsync(string correo, string password, CancellationToken ct = default)
    {
        _inputValidator.ValidarCorreo(correo);
        _inputValidator.ValidarPassword(password);

        correo = correo.Trim().ToLowerInvariant();

        var existe = await _dbContext.Usuarios
            .AnyAsync(u => u.Correo == correo, ct);

        if (existe)
            throw new AutenticacionException("El correo ya está registrado.");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Correo = correo,
            PasswordHash = _passwordHasher.Hash(password),
            Rol = RolUsuario.Estandar,
            EstaActivo = false,
            FechaCreacionUtc = DateTime.UtcNow,
            IntentosFallidos = 0
        };

        var tokenActivacion = new TokenActivacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiracionUtc = DateTime.UtcNow.AddHours(24),
            FueUsado = false
        };

        var enlaceActivacion = $"/api/auth/activar?token={tokenActivacion.Token}";
        var correoEnCola = new CorreoEnCola
        {
            Id = Guid.NewGuid(),
            Destinatario = correo,
            Asunto = "Activa tu cuenta",
            Cuerpo = $"Haz clic en el siguiente enlace para activar tu cuenta: {enlaceActivacion}",
            Estado = "Pendiente",
            FechaCreacionUtc = DateTime.UtcNow
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            _dbContext.Usuarios.Add(usuario);
            _dbContext.TokensActivacion.Add(tokenActivacion);
            _dbContext.CorreosEnCola.Add(correoEnCola);

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ActivarCuentaAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ValidacionException("El token es obligatorio.");

        var tokenActivacion = await _dbContext.TokensActivacion
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (tokenActivacion is null)
            throw new AutenticacionException("El token de activación no es válido.");

        if (tokenActivacion.FueUsado)
            throw new AutenticacionException("El token de activación ya fue utilizado.");

        if (tokenActivacion.ExpiracionUtc < DateTime.UtcNow)
            throw new AutenticacionException("El token de activación ha expirado.");

        tokenActivacion.FueUsado = true;

        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Id == tokenActivacion.UsuarioId, ct);

        if (usuario is null)
            throw new AutenticacionException("No se encontró el usuario asociado al token.");

        usuario.EstaActivo = true;

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task ReenviarActivacionAsync(string correo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return;

        correo = correo.Trim().ToLowerInvariant();

        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo, ct);

        if (usuario is null || usuario.EstaActivo)
            return;

        var tokensAnteriores = await _dbContext.TokensActivacion
            .Where(t => t.UsuarioId == usuario.Id && !t.FueUsado)
            .ToListAsync(ct);

        foreach (var t in tokensAnteriores)
        {
            t.FueUsado = true;
        }

        var nuevoToken = new TokenActivacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiracionUtc = DateTime.UtcNow.AddHours(24),
            FueUsado = false
        };

        var enlaceActivacion = $"/api/auth/activar?token={nuevoToken.Token}";
        var correoEnCola = new CorreoEnCola
        {
            Id = Guid.NewGuid(),
            Destinatario = correo,
            Asunto = "Activa tu cuenta",
            Cuerpo = $"Haz clic en el siguiente enlace para activar tu cuenta: {enlaceActivacion}",
            Estado = "Pendiente",
            FechaCreacionUtc = DateTime.UtcNow
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            _dbContext.TokensActivacion.Add(nuevoToken);
            _dbContext.CorreosEnCola.Add(correoEnCola);

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
