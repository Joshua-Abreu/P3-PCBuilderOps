using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Domain;

namespace PCBuilderOps.Data;

// RD-03: el Core no conoce este contexto; el módulo solo referencia la tabla Usuarios del Core.
public class PCBuilderOpsDbContext : DbContext
{
    public const string TablaHistorialMigraciones = "__MigracionesPCBuilderOps";

    public PCBuilderOpsDbContext(DbContextOptions<PCBuilderOpsDbContext> options) : base(options)
    {
    }

    public DbSet<OrdenEnsamble> OrdenesEnsamble => Set<OrdenEnsamble>();
    public DbSet<Componente> Componentes => Set<Componente>();
    public DbSet<DetalleEnsamble> DetallesEnsamble => Set<DetalleEnsamble>();
    public DbSet<PruebaRendimiento> PruebasRendimiento => Set<PruebaRendimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>().ToTable("Usuarios", tabla => tabla.ExcludeFromMigrations());
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PCBuilderOpsDbContext).Assembly);
    }
}
