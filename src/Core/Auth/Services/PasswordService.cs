using Core.Auth.Domain;
using Core.Data;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Auth.Services;

public class PasswordService : IPasswordService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInputValidator _inputValidator;

    public PasswordService(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IInputValidator inputValidator)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _inputValidator = inputValidator;
    }

    public async Task OlvidePasswordAsync(string correo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return;

        correo = correo.Trim().ToLowerInvariant();

        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo && u.EstaActivo, ct);

        if (usuario is null)
            return;

        var tokenRecuperacion = new TokenRecuperacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Token = Guid.NewGuid().ToString("N"),
            FechaCreacionUtc = DateTime.UtcNow,
            ExpiracionUtc = DateTime.UtcNow.AddHours(2),
            FueUsado = false
        };

        var enlaceRecuperacion = $"http://localhost:5206/api/auth/restablecer-password?token={tokenRecuperacion.Token}";
        var correoEnCola = new CorreoEnCola
        {
            Id = Guid.NewGuid(),
            Destinatario = correo,
            Asunto = "Recuperación de contraseña",
            Cuerpo = $"Haz clic en el siguiente enlace para restablecer tu contraseña: {enlaceRecuperacion}",
            Estado = "Pendiente",
            FechaCreacionUtc = DateTime.UtcNow
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            _dbContext.TokensRecuperacion.Add(tokenRecuperacion);
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

    public async Task RestablecerPasswordAsync(string token, string nuevaPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new Core.Auth.Exceptions.ValidacionException("El token es obligatorio.");

        _inputValidator.ValidarPassword(nuevaPassword);

        var tokenRecuperacion = await _dbContext.TokensRecuperacion
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (tokenRecuperacion is null)
            throw new Core.Auth.Exceptions.AutenticacionException("El token de recuperación no es válido.");

        if (tokenRecuperacion.FueUsado)
            throw new Core.Auth.Exceptions.AutenticacionException("El token de recuperación ya fue utilizado.");

        if (tokenRecuperacion.ExpiracionUtc < DateTime.UtcNow)
            throw new Core.Auth.Exceptions.AutenticacionException("El token de recuperación ha expirado.");

        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Id == tokenRecuperacion.UsuarioId, ct);

        if (usuario is null)
            throw new Core.Auth.Exceptions.AutenticacionException("No se encontró el usuario asociado al token.");

        usuario.PasswordHash = _passwordHasher.Hash(nuevaPassword);
        tokenRecuperacion.FueUsado = true;
        usuario.DebeCambiarPassword = false;
        usuario.IntentosFallidos = 0;

        var sesionesActivas = await _dbContext.SesionesUsuario
            .Where(s => s.UsuarioId == usuario.Id && !s.EstaRevocada)
            .ToListAsync(ct);

        foreach (var sesion in sesionesActivas)
        {
            sesion.EstaRevocada = true;
            sesion.FechaRevocacionUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task ForzarCambioPasswordAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId, ct);

        if (usuario is null)
            throw new Core.Auth.Exceptions.AutenticacionException("Usuario no encontrado.");

        usuario.DebeCambiarPassword = true;

        var sesionesActivas = await _dbContext.SesionesUsuario
            .Where(s => s.UsuarioId == usuarioId && !s.EstaRevocada)
            .ToListAsync(ct);

        foreach (var sesion in sesionesActivas)
        {
            sesion.EstaRevocada = true;
            sesion.FechaRevocacionUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
