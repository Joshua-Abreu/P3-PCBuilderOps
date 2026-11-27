using Core.Auditoria.Domain;
using Core.Auditoria.Exceptions;
using Core.Auth.Domain;
using Core.Data.Configurations;
using Core.Documentos.Domain;
using Core.Notifications.Domain;
using Core.Permisos.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenActivacion> TokensActivacion => Set<TokenActivacion>();
    public DbSet<CorreoEnCola> CorreosEnCola => Set<CorreoEnCola>();
    public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();
    public DbSet<TokenRecuperacion> TokensRecuperacion => Set<TokenRecuperacion>();
    public DbSet<SolicitudPermiso> SolicitudesPermiso => Set<SolicitudPermiso>();
    public DbSet<PermisoUsuario> PermisosUsuario => Set<PermisoUsuario>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<PreferenciaNotificacion> PreferenciasNotificacion => Set<PreferenciaNotificacion>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RechazarCambiosAlHistorial();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RechazarCambiosAlHistorial();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // RF-AUD-06
    private void RechazarCambiosAlHistorial()
    {
        if (ChangeTracker.Entries<RegistroAuditoria>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new HistorialInmutableException();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.UsarFechasUtc();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
