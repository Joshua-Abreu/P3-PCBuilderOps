using Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCBuilderOps.Domain;

namespace PCBuilderOps.Data.Configurations;

// FN-05, RF-NEG-08
public class TransicionOrdenConfiguration : IEntityTypeConfiguration<TransicionOrden>
{
    public void Configure(EntityTypeBuilder<TransicionOrden> builder)
    {
        builder.ToTable("TransicionesOrden");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.EstadoAnterior)
            .HasConversion<int?>();

        builder.Property(t => t.EstadoNuevo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.FechaUtc)
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hallazgo 2 (Asignación 3), CA-HIS-09
        builder.HasIndex(t => new { t.OrdenEnsambleId, t.EstadoNuevo })
            .IsUnique()
            .HasDatabaseName("UX_TransicionesOrden_UnaPorEstado");

        builder.HasIndex(t => new { t.EstadoNuevo, t.FechaUtc })
            .HasDatabaseName("IX_TransicionesOrden_EstadoNuevo_FechaUtc");
    }
}
