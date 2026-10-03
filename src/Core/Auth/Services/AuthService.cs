using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Data;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Auth.Services;

public class AuthService : IAuthService
{
    private const int MaxIntentosFallidos = 5;
    private const int BloqueoMinutos = 15;

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInputValidator _inputValidator;
    private readonly IJwtService _jwtService;

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IInputValidator inputValidator,
        IJwtService jwtService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _inputValidator = inputValidator;
        _jwtService = jwtService;
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

    public async Task<LoginResultado> LoginAsync(string correo, string password, CancellationToken ct = default)
    {
        _inputValidator.ValidarCorreo(correo);

        correo = correo.Trim().ToLowerInvariant();

        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo, ct);

        if (usuario is null)
            throw new AutenticacionException("Credenciales incorrectas.");

        if (!usuario.EstaActivo)
            throw new AutenticacionException("La cuenta no está activa.");

        if (usuario.BloqueadoHastaUtc.HasValue && usuario.BloqueadoHastaUtc.Value > DateTime.UtcNow)
            throw new AutenticacionException("La cuenta está bloqueada temporalmente.");

        if (!_passwordHasher.Verificar(password, usuario.PasswordHash))
        {
            usuario.IntentosFallidos++;

            if (usuario.IntentosFallidos >= MaxIntentosFallidos)
            {
                usuario.BloqueadoHastaUtc = DateTime.UtcNow.AddMinutes(BloqueoMinutos);
                usuario.IntentosFallidos = 0;
            }

            await _dbContext.SaveChangesAsync(ct);
            throw new AutenticacionException("Credenciales incorrectas.");
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHastaUtc = null;

        var tokenJwt = _jwtService.GenerarToken(usuario.Id, usuario.Correo, usuario.Rol);

        var sesion = new SesionUsuario
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenJwt = tokenJwt,
            FechaCreacionUtc = DateTime.UtcNow,
            ExpiracionUtc = DateTime.UtcNow.AddHours(1),
            EstaRevocada = false
        };

        _dbContext.SesionesUsuario.Add(sesion);
        await _dbContext.SaveChangesAsync(ct);

        return new LoginResultado(tokenJwt, sesion.ExpiracionUtc, usuario.DebeCambiarPassword);
    }

    public async Task LogoutAsync(string tokenJwt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tokenJwt))
            return;

        var sesion = await _dbContext.SesionesUsuario
            .FirstOrDefaultAsync(s => s.TokenJwt == tokenJwt && !s.EstaRevocada, ct);

        if (sesion is null)
            return;

        sesion.EstaRevocada = true;
        sesion.FechaRevocacionUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<UsuarioAutenticado?> ObtenerUsuarioAutenticadoAsync(CancellationToken ct = default)
    {
        var sesion = await _dbContext.SesionesUsuario
            .Where(s => !s.EstaRevocada && s.ExpiracionUtc > DateTime.UtcNow)
            .OrderByDescending(s => s.FechaCreacionUtc)
            .FirstOrDefaultAsync(ct);

        if (sesion is null)
            return null;

        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Id == sesion.UsuarioId, ct);

        if (usuario is null)
            return null;

        return new UsuarioAutenticado(usuario.Id, usuario.Correo, usuario.Rol);
    }
}
