using Core.Auth.Domain;
using Core.Data;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Core.Auth.Services;

public class PasswordService : IPasswordService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInputValidator _inputValidator;
    private readonly IConfiguration _configuration;

    public PasswordService(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IInputValidator inputValidator,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _inputValidator = inputValidator;
        _configuration = configuration;
    }

    private string ObtenerBaseUrl() => UrlBaseAplicacion.Obtener(_configuration);

    public async Task OlvidePasswordAsync(string correo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return;

        correo = correo.Trim().ToLowerInvariant();

        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo && u.EstaActivo, ct);

        if (usuario is null)
            return;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            EncolarCorreoRecuperacion(
                usuario,
                "Recuperación de contraseña",
                "Haz clic en el siguiente enlace para restablecer tu contraseña");

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ValidarTokenRecuperacionAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new Core.Auth.Exceptions.ValidacionException("El token es obligatorio.");

        await ObtenerTokenRecuperacionValidoAsync(token, ct);
    }

    private async Task<TokenRecuperacion> ObtenerTokenRecuperacionValidoAsync(string token, CancellationToken ct)
    {
        var tokenRecuperacion = await _dbContext.TokensRecuperacion
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (tokenRecuperacion is null)
            throw new Core.Auth.Exceptions.AutenticacionException("El token de recuperación no es válido.");

        if (tokenRecuperacion.FueUsado)
            throw new Core.Auth.Exceptions.AutenticacionException("El token de recuperación ya fue utilizado.");

        if (tokenRecuperacion.ExpiracionUtc < DateTime.UtcNow)
            throw new Core.Auth.Exceptions.AutenticacionException("El token de recuperación ha expirado.");

        return tokenRecuperacion;
    }

    public async Task RestablecerPasswordAsync(string token, string nuevaPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new Core.Auth.Exceptions.ValidacionException("El token es obligatorio.");

        _inputValidator.ValidarPassword(nuevaPassword);

        var tokenRecuperacion = await ObtenerTokenRecuperacionValidoAsync(token, ct);

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

    // RF-CA-13
    public async Task ForzarRestablecimientoAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId, ct);

        if (usuario is null)
            throw new Core.Auth.Exceptions.AutenticacionException("Usuario no encontrado.");

        usuario.PasswordHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N"));
        usuario.DebeCambiarPassword = true;

        var tokensPendientes = await _dbContext.TokensRecuperacion
            .Where(t => t.UsuarioId == usuarioId && !t.FueUsado)
            .ToListAsync(ct);

        foreach (var token in tokensPendientes)
        {
            token.FueUsado = true;
        }

        EncolarCorreoRecuperacion(
            usuario,
            "Restablecimiento de contraseña requerido",
            "Un administrador restableció tu contraseña. Define una nueva desde el siguiente enlace");

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

    public async Task CambiarPasswordAsync(Guid usuarioId, string passwordActual, string nuevaPassword, CancellationToken ct = default)
    {
        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId, ct);

        if (usuario is null)
            throw new Core.Auth.Exceptions.AutenticacionException("Usuario no encontrado.");

        if (!_passwordHasher.Verificar(passwordActual, usuario.PasswordHash))
            throw new Core.Auth.Exceptions.ValidacionException("La contraseña actual es incorrecta.");

        _inputValidator.ValidarPassword(nuevaPassword);

        usuario.PasswordHash = _passwordHasher.Hash(nuevaPassword);
        usuario.DebeCambiarPassword = false;
        usuario.IntentosFallidos = 0;

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

    private void EncolarCorreoRecuperacion(Usuario usuario, string asunto, string mensaje)
    {
        var tokenRecuperacion = new TokenRecuperacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Token = Guid.NewGuid().ToString("N"),
            FechaCreacionUtc = DateTime.UtcNow,
            ExpiracionUtc = DateTime.UtcNow.AddHours(2),
            FueUsado = false
        };

        var enlaceRecuperacion = $"{ObtenerBaseUrl()}/api/auth/restablecer-password?token={tokenRecuperacion.Token}";
        var correoEnCola = new CorreoEnCola
        {
            Id = Guid.NewGuid(),
            Destinatario = usuario.Correo,
            Asunto = asunto,
            Cuerpo = $"{mensaje}: {enlaceRecuperacion}",
            Estado = "Pendiente",
            FechaCreacionUtc = DateTime.UtcNow
        };

        _dbContext.TokensRecuperacion.Add(tokenRecuperacion);
        _dbContext.CorreosEnCola.Add(correoEnCola);
    }
}
