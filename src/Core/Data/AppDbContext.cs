using Core.Auth.Domain;
using Core.Data.Configurations;
using Core.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

/// <summary>
/// Contexto de base de datos de la aplicación.
/// Configura las entidades del Core mediante Fluent API.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenActivacion> TokensActivacion => Set<TokenActivacion>();
    public DbSet<CorreoEnCola> CorreosEnCola => Set<CorreoEnCola>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configuraciones Fluent API desde este ensamblado
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
