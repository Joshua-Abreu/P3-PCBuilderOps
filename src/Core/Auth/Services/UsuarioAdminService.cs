using Core.Auth.Domain;
using Core.Auth.Exceptions;
using Core.Data;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Auth.Services;

public class UsuarioAdminService : IUsuarioAdminService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInputValidator _inputValidator;

    public UsuarioAdminService(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IInputValidator inputValidator)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _inputValidator = inputValidator;
    }

    public async Task<PaginacionUsuarios> ListarAsync(int pagina, int tamanoPagina, RolUsuario? rol, bool? activo, CancellationToken ct = default)
    {
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 1) tamanoPagina = 10;
        if (tamanoPagina > 100) tamanoPagina = 100;

        var query = _dbContext.Usuarios.AsQueryable();

        if (rol.HasValue)
            query = query.Where(u => u.Rol == rol.Value);

        if (activo.HasValue)
            query = query.Where(u => u.EstaActivo == activo.Value);

        var total = await query.CountAsync(ct);

        var usuarios = await query
            .OrderBy(u => u.Correo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(u => new UsuarioDetalle(u.Id, u.Correo, u.Rol.ToString(), u.EstaActivo, u.FechaCreacionUtc, u.DebeCambiarPassword))
            .ToListAsync(ct);

        return new PaginacionUsuarios(total, pagina, tamanoPagina, usuarios);
    }

    public async Task<UsuarioDetalle?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (usuario is null)
            return null;

        return new UsuarioDetalle(usuario.Id, usuario.Correo, usuario.Rol.ToString(), usuario.EstaActivo, usuario.FechaCreacionUtc, usuario.DebeCambiarPassword);
    }

    public async Task<UsuarioDetalle> CrearAsync(string correo, string password, RolUsuario rol, bool activarInmediatamente, CancellationToken ct = default)
    {
        _inputValidator.ValidarCorreo(correo);
        _inputValidator.ValidarPassword(password);

        correo = correo.Trim().ToLowerInvariant();

        var existe = await _dbContext.Usuarios
            .AnyAsync(u => u.Correo == correo, ct);

        if (existe)
            throw new ReglaNegocioException("El correo ya está registrado.");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Correo = correo,
            PasswordHash = _passwordHasher.Hash(password),
            Rol = rol,
            EstaActivo = activarInmediatamente,
            FechaCreacionUtc = DateTime.UtcNow,
            IntentosFallidos = 0
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            _dbContext.Usuarios.Add(usuario);

            if (!activarInmediatamente)
            {
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

                _dbContext.TokensActivacion.Add(tokenActivacion);
                _dbContext.CorreosEnCola.Add(correoEnCola);
            }

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new UsuarioDetalle(usuario.Id, usuario.Correo, usuario.Rol.ToString(), usuario.EstaActivo, usuario.FechaCreacionUtc, usuario.DebeCambiarPassword);
    }

    public async Task CambiarRolAsync(Guid id, RolUsuario nuevoRol, Guid adminId, CancellationToken ct = default)
    {
        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (usuario is null)
            throw new AutenticacionException("Usuario no encontrado.");

        if (usuario.Id == adminId && usuario.Rol == RolUsuario.Administrador && nuevoRol != RolUsuario.Administrador)
        {
            var adminsActivos = await _dbContext.Usuarios
                .CountAsync(u => u.Rol == RolUsuario.Administrador && u.EstaActivo && u.Id != adminId, ct);

            if (adminsActivos == 0)
                throw new ReglaNegocioException("No puedes remover tu rol de Administrador si eres el único admin activo.");
        }

        usuario.Rol = nuevoRol;
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task CambiarEstadoAsync(Guid id, bool activar, Guid adminId, CancellationToken ct = default)
    {
        var usuario = await _dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (usuario is null)
            throw new AutenticacionException("Usuario no encontrado.");

        if (usuario.Id == adminId && !activar)
            throw new ReglaNegocioException("No puedes desactivarte a ti mismo.");

        usuario.EstaActivo = activar;

        if (!activar)
        {
            var sesionesActivas = await _dbContext.SesionesUsuario
                .Where(s => s.UsuarioId == id && !s.EstaRevocada)
                .ToListAsync(ct);

            foreach (var sesion in sesionesActivas)
            {
                sesion.EstaRevocada = true;
                sesion.FechaRevocacionUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
