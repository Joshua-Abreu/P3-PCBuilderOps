using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCBuilderOps.Domain;

namespace PCBuilderOps.Data.Configurations;

public class OrdenEnsambleConfiguration : IEntityTypeConfiguration<OrdenEnsamble>
{
    public void Configure(EntityTypeBuilder<OrdenEnsamble> builder)
    {
        builder.ToTable("OrdenesEnsamble");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.CodigoSeguimiento)
            .IsRequired()
            .HasMaxLength(OrdenEnsamble.CodigoSeguimientoMaximo);

        builder.HasIndex(o => o.CodigoSeguimiento)
            .IsUnique()
            .HasDatabaseName("IX_OrdenesEnsamble_CodigoSeguimiento");

        builder.Property(o => o.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(o => o.CostoTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.FechaCreacionUtc)
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(o => o.ClienteUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(o => o.TecnicoAsignadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Detalles)
            .WithOne()
            .HasForeignKey(d => d.OrdenEnsambleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Pruebas)
            .WithOne()
            .HasForeignKey(p => p.OrdenEnsambleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.Estado)
            .HasDatabaseName("IX_OrdenesEnsamble_Estado");
    }
}
