using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PCBuilderOps.Domain;

namespace PCBuilderOps.Data.Configurations;

public class DetalleEnsambleConfiguration : IEntityTypeConfiguration<DetalleEnsamble>
{
    public void Configure(EntityTypeBuilder<DetalleEnsamble> builder)
    {
        builder.ToTable("DetallesEnsamble");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.Cantidad)
            .IsRequired();

        builder.Property(d => d.PrecioUnitario)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(d => d.Componente)
            .WithMany()
            .HasForeignKey(d => d.ComponenteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
